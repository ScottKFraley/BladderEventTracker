import os
import uuid
import re
from datetime import datetime
import psycopg2
from msgraph.generated.users.users_request_builder import UsersRequestBuilder
from kiota_http.httpx_request_adapter import HttpxRequestAdapter
from azure.identity import DeviceCodeCredential
from msgraph import GraphServiceClient


class OneNoteExtractor:
    def __init__(self):
        client_id = os.getenv("AZURE_CLIENT_ID")
        tenant_id = os.getenv("AZURE_TENANT_ID")

        credential = DeviceCodeCredential(client_id=client_id, tenant_id=tenant_id)

        request_adapter = HttpxRequestAdapter(credential)
        self.client = GraphServiceClient(request_adapter)
        self.user_id = None
        self.db_conn = None

    def connect_to_db(self):
        # Get connection details from environment variables
        conn_params = {
            "dbname": os.getenv("PG_DATABASE"),
            "user": os.getenv("PG_USER"),
            "password": os.getenv("PG_PASSWORD"),
            "host": os.getenv("PG_HOST"),
            "port": os.getenv("PG_PORT", "5432"),
        }
        self.db_conn = psycopg2.connect(**conn_params)

    async def get_notebooks(self):
        response = await self.client.me.onenote.notebooks.get()
        return response.value if response else []

    async def get_sections(self, notebook_id):
        response = await self.client.me.onenote.notebooks.by_notebook_id(
            notebook_id
        ).sections.get()
        return response.value if response else []

    async def get_pages(self, section_id):
        response = await self.client.me.onenote.sections.by_section_id(
            section_id
        ).pages.get()
        return response.value if response else []

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
            print(f"{idx + 1}. {notebook.display_name}")

        notebook_idx = int(input("Select notebook number: ")) - 1
        selected_notebook = notebooks[notebook_idx]

        sections = await self.get_sections(selected_notebook.id)
        print("\nAvailable sections:")
        for idx, section in enumerate(sections):
            print(f"{idx + 1}. {section.display_name}")

        section_idx = int(input("Select section number: ")) - 1
        selected_section = sections[section_idx]

        self.user_id = input("Enter User ID (UUID format): ")
        try:
            uuid.UUID(self.user_id)
        except ValueError:
            raise ValueError("Invalid UUID format")

        pages = await self.get_pages(selected_section.id)
        cursor = self.db_conn.cursor()

        for page in pages:
            event_date = datetime.strptime(page.title, "%Y-%m-%d").date()
            content_response = await self.client.me.onenote.pages.by_page_id(
                page.id
            ).content.get()
            content = content_response.text

            entries = self.parse_page_content(content, event_date)

            for entry in entries:
                sql, values = self.generate_insert_statement(entry)
                cursor.execute(sql, values)

        self.db_conn.commit()
        cursor.close()


async def main():
    extractor = OneNoteExtractor()
    extractor.connect_to_db()
    await extractor.process_notebook()


if __name__ == "__main__":
    import asyncio

    asyncio.run(main())
