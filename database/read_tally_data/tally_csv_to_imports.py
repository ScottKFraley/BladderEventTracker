import csv
import sys
import datetime

def urgency_to_int(text):
    urgency_map = {
        "0 - No real urgency": 0,
        "1 - Slight Urgency": 1,
        "2 - Pretty Urgent": 2,
        "3 - Very Urgent": 3
    }
    return urgency_map.get(text, 1)  # Default to 1 if not found

def leak_to_int(text):
    leak_map = {
        "1 - Slight": 1, 
        "2 - Moderate": 2, 
        "3 - Heavy": 3
    }
    return leak_map.get(text, 1)  # Default to 1 if not found


def yesNo_to_bool(input_text):
    if input_text and input_text.lower() in ['yes', 'y', 'true', '1']:
        return 'true'
    return 'false'


def format_datetime(date_str, time_str):
    # Parse date and time from the format in the CSV
    try:
        # Parse the date (format: YYYY-MM-DD)
        date_obj = datetime.datetime.strptime(date_str, "%Y-%m-%d")
        
        # Parse the time (format: H:MM or HH:MM)
        time_parts = time_str.split(':')
        hour = int(time_parts[0])
        minute = int(time_parts[1])

        # Combine date and time as a naive datetime
        naive_datetime = datetime.datetime(date_obj.year, date_obj.month, date_obj.day, hour, minute)
          
        # Format for PostgreSQL with explicit timezone info
        return naive_datetime.isoformat()
    
    except Exception as e:
        print(f"Error parsing date/time: {date_str} {time_str} - {str(e)}")
        return None


def start_parsing_datafile(input_file):
    try:
        # The UUID for the user
        USER_ID = "8e3ddf21-4153-4838-abc6-47d553a5d905"
        
        # Output file name based on input file
        output_file = input_file.rsplit('.', 1)[0] + "_output.sql"
        
        # Open your input and output files
        with open(input_file, "r", encoding="utf-8-sig") as infile, open(output_file, "w", encoding="utf-8") as outfile:
            print(f"Processing {input_file}...")
            
            # Create a CSV reader
            reader = csv.DictReader(infile)
            
            # Write the INSERT statement header
            outfile.write(
                """INSERT INTO public."TrackingLog" (
                "EventDate",
                "Accident",
                "ChangePadOrUnderware",
                "LeakAmount",
                "Urgency",
                "AwokeFromSleep",
                "PainLevel",
                "Notes",
                "UserId"
            ) VALUES\n"""
            )
            
            # Process each row
            first_row = True
            for row in reader:
                # Map CSV fields to database fields
                event_date = row['Event Date']
                event_time = row['Event Time']
                
                # Format datetime
                formatted_datetime = format_datetime(event_date, event_time)
                if not formatted_datetime:
                    # skip bad row(s)
                    continue
                
                # Map other fields
                accident = yesNo_to_bool(row['Did you have an accident?'])
                change_pad = yesNo_to_bool(row.get('Did you have to change your pad/underwear?', 'No'))
                leak_amount = leak_to_int(row['Leak Amount'])
                urgency = urgency_to_int(row['Urgency'])
                awoke_from_sleep = yesNo_to_bool(row['Were you sleeping?'])
                pain_level = row['Pain Level']
                notes = row['Notes']
                
                # Escape any single quotes in notes
                if notes:
                    escaped_notes = notes.replace("'", "''")
                    notes_value = f"'{escaped_notes}'"
                else:
                    notes_value = 'DEFAULT'
                
                # Format the values
                values = f"(TIMESTAMP '{formatted_datetime}', " \
                        f"{accident}, " \
                        f"{change_pad}, " \
                        f"{leak_amount}, " \
                        f"{urgency}, " \
                        f"{awoke_from_sleep}, " \
                        f"{pain_level if pain_level else 'DEFAULT'}, " \
                        f"{notes_value}, " \
                        f"'{USER_ID}')"
                
                # Add comma separator if not the first row
                if not first_row:
                    outfile.write(", \n")
                else:
                    first_row = False
                
                outfile.write(values)
            
            # End the statement
            outfile.write(";\n")
            print(f"SQL file generated successfully: {output_file}")
    
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
