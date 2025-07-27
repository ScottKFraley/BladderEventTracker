import csv
import sys
import datetime


def urgency_to_int(text):
    urgency_map = {
        "0 - No real urgency": 0,
        "1 - Slight Urgency": 1,
        "2 - Pretty Urgent": 2,
        "3 - Very Urgent": 3,
    }
    return urgency_map.get(text, 1)  # Default to 1 if not found


def leak_to_int(text):
    leak_map = {"1 - Slight": 1, "2 - Moderate": 2, "3 - Heavy": 3}
    return leak_map.get(text, 1)  # Default to 1 if not found


def yesNo_to_bool(input_text):
    """Convert yes/no text to SQL Server bit values (1/0)"""
    if input_text and input_text.lower() in ["yes", "y", "true", "1"]:
        return 1
    return 0


def format_datetime(date_str, time_str):
    """Format datetime for SQL Server compatibility"""
    try:
        # Parse the date (format: YYYY-MM-DD)
        date_obj = datetime.datetime.strptime(date_str, "%Y-%m-%d")

        # Parse the time (format: H:MM or HH:MM)
        time_parts = time_str.split(":")
        hour = int(time_parts[0])
        minute = int(time_parts[1])

        # Combine date and time as a naive datetime
        naive_datetime = datetime.datetime(
            date_obj.year, date_obj.month, date_obj.day, hour, minute
        )

        # Format for SQL Server datetime - no timezone needed for your use case
        return naive_datetime.strftime("%Y-%m-%d %H:%M:%S")

    except Exception as e:
        print(f"Error parsing date/time: {date_str} {time_str} - {str(e)}")
        return None


# The UUID for the user
# USER_ID = "91A77400-564E-4312-8DB5-BCD869A786CE"
def start_parsing_datafile(input_file):
    try:
        # The UUID for the user
        USER_ID = "91A77400-564E-4312-8DB5-BCD869A786CE"

        # SQL Server has a 1000 row limit for VALUES clauses
        MAX_ROWS_PER_FILE = 1000

        print(f"Processing {input_file}...")

        # Read all rows first to determine how many files we need
        with open(input_file, "r", encoding="utf-8-sig") as infile:
            reader = csv.DictReader(infile)
            all_rows = list(reader)

        total_rows = len(all_rows)
        total_files = (
            total_rows + MAX_ROWS_PER_FILE - 1
        ) // MAX_ROWS_PER_FILE  # Ceiling division

        print(f"Total rows: {total_rows}, will create {total_files} file(s)")

        # Process rows in batches
        for file_num in range(total_files):
            start_row = file_num * MAX_ROWS_PER_FILE
            end_row = min(start_row + MAX_ROWS_PER_FILE, total_rows)

            # Generate output filename
            if total_files == 1:
                output_file = input_file.rsplit(".", 1)[0] + "_output.sql"
            else:
                output_file = (
                    input_file.rsplit(".", 1)[0] + f"_output_part{file_num + 1:02d}.sql"
                )

            with open(output_file, "w", encoding="utf-8") as outfile:
                # Write the INSERT statement header - SQL Server syntax
                outfile.write(
                    """INSERT INTO [TrackingLog] (
                    [EventDate],
                    [Accident],
                    [ChangePadOrUnderware],
                    [LeakAmount],
                    [Urgency],
                    [AwokeFromSleep],
                    [PainLevel],
                    [Notes],
                    [UserId]
                ) VALUES\n"""
                )

                # Process rows for this file
                first_row = True
                rows_in_file = 0

                for i in range(start_row, end_row):
                    row = all_rows[i]

                    # Map CSV fields to database fields
                    event_date = row["Event Date"]
                    event_time = row["Event Time"]

                    # Format datetime
                    formatted_datetime = format_datetime(event_date, event_time)
                    if not formatted_datetime:
                        # skip bad row(s)
                        continue

                    # Map other fields - now returns 1/0 for SQL Server bit fields
                    accident = yesNo_to_bool(row["Did you have an accident?"])
                    change_pad = yesNo_to_bool(
                        row.get("Did you have to change your pad/underwear?", "No")
                    )
                    leak_amount = leak_to_int(row["Leak Amount"])
                    urgency = urgency_to_int(row["Urgency"])
                    awoke_from_sleep = yesNo_to_bool(row["Were you sleeping?"])
                    pain_level = row["Pain Level"]
                    notes = row["Notes"]

                    # Escape any single quotes in notes for SQL Server
                    if notes:
                        escaped_notes = notes.replace("'", "''")
                        notes_value = f"'{escaped_notes}'"
                    else:
                        notes_value = (
                            "NULL"  # Use NULL instead of DEFAULT for SQL Server
                        )

                    # Format the values - SQL Server syntax
                    values = (
                        f"('{formatted_datetime}', "
                        f"{accident}, "
                        f"{change_pad}, "
                        f"{leak_amount}, "
                        f"{urgency}, "
                        f"{awoke_from_sleep}, "
                        f"{pain_level if pain_level else 'NULL'}, "
                        f"{notes_value}, "
                        f"'{USER_ID}')"
                    )

                    # Add comma separator if not the first row
                    if not first_row:
                        outfile.write(", \n")
                    else:
                        first_row = False

                    outfile.write(values)
                    rows_in_file += 1

                # End the statement
                outfile.write(";\n")
                print(f"Created {output_file} with {rows_in_file} rows")

        print(f"All SQL files generated successfully!")

    except FileNotFoundError:
        print(f"Error: The file '{input_file}' was not found.")
        return False
    except PermissionError:
        print(f"Error: Permission denied accessing '{input_file}'")
        return False
    except Exception as e:
        print(f"An error occurred: {str(e)}")
        return False

    return True


if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Usage: python script.py <input_csv_file>")
        sys.exit(1)

    input_file = sys.argv[1]
    if start_parsing_datafile(input_file):
        print("Processing completed successfully.")
    else:
        print("Processing failed.")
        sys.exit(1)
