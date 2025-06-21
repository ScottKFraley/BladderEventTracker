import os
import uuid
import re
from datetime import datetime
import asyncio
import psycopg2
import requests
import msal


class OneNoteExtractor:
    def __init__(self):
        # Get app credentials from environment variables
        self.client_id = os.getenv("AZURE_CLIENT_ID")
        self.tenant_id = os.getenv("AZURE_TENANT_ID")
        self.client_secret = os.getenv(
            "AZURE_CLIENT_SECRET"
        )  # You'll need to set this env var

        self.graph_url = "https://graph.microsoft.com/v1.0"
        self.user_id = None
        self.db_conn = None
        self.access_token = None

        # Create an MSAL app
        # Update the authority to include 'common' for multi-tenant apps
        self.app = msal.PublicClientApplication(
            client_id=self.client_id,
            authority=f"https://login.microsoftonline.com/{self.tenant_id}",
        )

        # Add these scopes for OneNote access
        self.scopes = ["Notes.Read.All", "Notes.ReadWrite.All"]


    async def get_token(self):
        # Try to get token from cache first
        accounts = self.app.get_accounts()
        if accounts:
            result = self.app.acquire_token_silent(self.scopes, account=accounts[0])
            if result:
                self.access_token = result["access_token"]
                return self.access_token

        # If no cached token, start interactive login
        result = self.app.acquire_token_interactive(scopes=self.scopes)

        if "access_token" in result:
            self.access_token = result["access_token"]
            return self.access_token
        else:
            print(f"Error getting token: {result.get('error')}")
            print(f"Error description: {result.get('error_description')}")
            raise Exception("Failed to acquire token")

    async def make_graph_request(self, endpoint):
        if not self.access_token:
            await self.get_token()

        headers = {
            "Authorization": f"Bearer {self.access_token}",
            "Content-Type": "application/json",
        }

        response = requests.get(f"{self.graph_url}{endpoint}", headers=headers)

        # Handle token expiration
        if response.status_code == 401:
            # Refresh token and retry
            await self.get_token()
            headers["Authorization"] = f"Bearer {self.access_token}"
            response = requests.get(f"{self.graph_url}{endpoint}", headers=headers)

        return response

    def connect_to_db(self):
        # Get connection details from environment variables
        conn_params = {
            "dbname": os.getenv("PG_DATABASE"),
            "user": os.getenv("PG_USER"),
            "password": os.getenv("PG_PASSWORD"),
            "host": os.getenv("PG_HOST", "localhost"),
            "port": os.getenv("PG_PORT", "5432"),
        }
        self.db_conn = psycopg2.connect(**conn_params)

    async def get_notebooks(self):
        response = await self.make_graph_request("/me/onenote/notebooks")
        if response.status_code == 200:
            return response.json().get("value", [])

        print(f"Error getting notebooks: {response.status_code} - {response.text}")

        return []

    async def get_sections(self, notebook_id):
        response = await self.make_graph_request(
            f"/me/onenote/notebooks/{notebook_id}/sections"
        )
        if response.status_code == 200:
            return response.json().get("value", [])

        print(f"Error getting sections: {response.status_code} - {response.text}")

        return []

    async def get_pages(self, section_id):
        response = await self.make_graph_request(
            f"/me/onenote/sections/{section_id}/pages"
        )
        if response.status_code == 200:
            return response.json().get("value", [])
        print(f"Error getting pages: {response.status_code} - {response.text}")

        return []

    async def get_page_content(self, page_id):
        response = await self.make_graph_request(f"/me/onenote/pages/{page_id}/content")
        if response.status_code == 200:
            return response.text

        print(f"Error getting page content: {response.status_code} - {response.text}")

        return ""

    def parse_page_content(self, page_content, event_date):
        entries = []
        time_pattern = r"^(\d{2}:\d{2})"
        pain_pattern = r"pain:(\d+)"
        urgency_pattern = r"urgency:(\d+)"
        notes_pattern = r'notes:"([^"]*)"'

        for line in page_content.split("\n"):
            line = line.strip()
            time_match = re.match(time_pattern, line)
            if not time_match:
                continue

            time_str = time_match.group(1)
            timestamp = f"{event_date} {time_str}"

            entry = {
                "Id": str(uuid.uuid4()),
                "EventDate": timestamp,
                "Accident": False,
                "ChangePadOrUnderware": False,
                "LeakAmount": 1,
                "Urgency": 1,
                "AwokeFromSleep": False,
                "PainLevel": 1,
                "Notes": None,
                "UserId": self.user_id,
            }

            pain_match = re.search(pain_pattern, line)
            if pain_match:
                entry["PainLevel"] = int(pain_match.group(1))

            urgency_match = re.search(urgency_pattern, line)
            if urgency_match:
                entry["Urgency"] = int(urgency_match.group(1))

            notes_match = re.search(notes_pattern, line)
            if notes_match:
                entry["Notes"] = notes_match.group(1)

            entries.append(entry)

        return entries

    def generate_insert_statement(self, entry):
        columns = entry.keys()
        values = [entry[col] for col in columns]
        placeholders = [f"%s" for _ in values]

        sql = f"""
        INSERT INTO public."TrackingLog" ({', '.join(f'"{col}"' for col in columns)})
        VALUES ({', '.join(placeholders)})
        """
        return sql, values

    async def process_notebook(self):
        notebooks = await self.get_notebooks()

        print("Available notebooks:")
        for idx, notebook in enumerate(notebooks):
            print(f"{idx + 1}. {notebook['displayName']}")

        notebook_idx = int(input("Select notebook number: ")) - 1
        selected_notebook = notebooks[notebook_idx]

        sections = await self.get_sections(selected_notebook["id"])
        print("\nAvailable sections:")
        for idx, section in enumerate(sections):
            print(f"{idx + 1}. {section['displayName']}")

        section_idx = int(input("Select section number: ")) - 1
        selected_section = sections[section_idx]

        self.user_id = input("Enter User ID (UUID format): ")
        try:
            uuid.UUID(self.user_id)
        except ValueError:
            raise ValueError("Invalid UUID format")

        pages = await self.get_pages(selected_section["id"])
        cursor = self.db_conn.cursor()

        for page in pages:
            try:
                event_date = datetime.strptime(page["title"], "%Y-%m-%d").date()
                content = await self.get_page_content(page["id"])

                entries = self.parse_page_content(content, event_date)
                print(f"Processing page {page['title']} - found {len(entries)} entries")

                for entry in entries:
                    sql, values = self.generate_insert_statement(entry)
                    cursor.execute(sql, values)

                self.db_conn.commit()
                print(f"Successfully imported data from {page['title']}")

            except Exception as e:
                self.db_conn.rollback()
                print(f"Error processing page {page['title']}: {str(e)}")

        cursor.close()


async def main():
    extractor = OneNoteExtractor()
    extractor.connect_to_db()
    await extractor.process_notebook()


if __name__ == "__main__":
    asyncio.run(main())
