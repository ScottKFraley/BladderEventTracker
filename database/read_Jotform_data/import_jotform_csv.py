import sys
import csv
import uuid
from datetime import datetime


def parse_jotform_datetime(date_str):
    """Parse Jotform datetime format 'Mar 6, 2025 03:46 PM' to datetime object"""
    try:
        return datetime.strptime(date_str.strip(), "%b %d, %Y %I:%M %p")
    except ValueError:
        return None


def convert_yes_no_to_bit(value):
    """Convert Yes/No responses to 1/0 bit values"""
    if not value or value.strip() == "":
        return 0
    return 1 if value.strip().lower() == "yes" else 0


def safe_int(value, default=0):
    """Safely convert value to int with default"""
    if not value or value.strip() == "":
        return default
    try:
        return int(float(value.strip()))
    except ValueError:
        return default


def write_batch(outfile, rows):
    """Write a batch of rows to the output file"""
    outfile.write(
        """INSERT INTO [TrackingLog] (
        [Id],
        [UserId],
        [EventDate],
        [Accident],
        [ChangePadOrUnderware],
        [LeakAmount],
        [Urgency],
        [AwokeFromSleep],
        [PainLevel],
        [Notes]
      ) VALUES\n"""
    )
    
    for i, row in enumerate(rows):
        if i > 0:
            outfile.write(",\n")
        outfile.write(row)
    
    outfile.write(";\n\n")


def process_jotform_csv(input_filename):
    """Process Jotform CSV and convert to SQL Server format"""
    try:
        USER_ID = "688E6E82-75F3-451F-8A0B-40176C70F7F8"
        
        with open(input_filename, 'r', newline='', encoding='utf-8') as infile, \
             open("Jotform_data_for_input.sql", "w", encoding='utf-8') as outfile:
            
            print("File opened successfully.")
            
            # Use comma delimiter for CSV format
            csv_reader = csv.DictReader(infile, delimiter=',')
            
            rows = []
            batch_size = 1000
            processed_count = 0
            
            for row in csv_reader:
                try:
                    # Map CSV columns to database fields
                    event_date_str = row.get("Event Date", "").strip()
                    if not event_date_str:
                        print(f"Skipping row {processed_count + 1}: No event date")
                        continue
                    
                    # Parse datetime
                    event_date = parse_jotform_datetime(event_date_str)
                    if not event_date:
                        print(f"Skipping row {processed_count + 1}: Invalid date format: {event_date_str}")
                        continue
                    
                    # Extract and convert field values
                    accident = convert_yes_no_to_bit(row.get("Did you have an accident", ""))
                    change_pad = convert_yes_no_to_bit(row.get("Did you have to change your underwear?", ""))
                    leak_amount = safe_int(row.get("Leak Amount", ""), 0)
                    urgency = safe_int(row.get("Urgency", ""), 1)
                    awoke_from_sleep = convert_yes_no_to_bit(row.get("Did this awaken you from sleep?", ""))
                    pain_level = safe_int(row.get("Pain level, if any", ""), 0)
                    notes = row.get("Notes", "").strip()
                    
                    # Handle notes - escape single quotes and wrap appropriately
                    if notes:
                        escaped_notes = notes.replace("'", "''")
                        notes_value = f"'{escaped_notes}'"
                    else:
                        notes_value = 'NULL'
                    
                    # Generate UUID for Id column
                    record_id = str(uuid.uuid4()).upper()
                    
                    # Create SQL row with proper datetime2(7) format
                    values = f"('{record_id}', " \
                            f"'{USER_ID}', " \
                            f"'{event_date.strftime('%Y-%m-%d %H:%M:%S.%f')[:-3]}', " \
                            f"{accident}, " \
                            f"{change_pad}, " \
                            f"{leak_amount}, " \
                            f"{urgency}, " \
                            f"{awoke_from_sleep}, " \
                            f"{pain_level}, " \
                            f"{notes_value})"
                    
                    rows.append(values)
                    processed_count += 1
                    
                    # Write batch when we reach batch_size
                    if len(rows) >= batch_size:
                        write_batch(outfile, rows)
                        print(f"Processed {processed_count} rows...")
                        rows = []
                
                except Exception as e:
                    print(f"Error processing row {processed_count + 1}: {str(e)}")
                    continue
            
            # Write any remaining rows
            if rows:
                write_batch(outfile, rows)
            
            print(f"SQL file generated successfully. Processed {processed_count} total rows.")
            return True
    
    except FileNotFoundError:
        print(f"Error: The file '{input_filename}' was not found.")
        return False
    except PermissionError:
        print(f"Error: Permission denied accessing '{input_filename}'")
        return False
    except Exception as e:
        print(f"An error occurred: {str(e)}")
        return False


# Main execution
if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Usage: python import_jotform_csv.py <input_filename>")
        sys.exit(1)
    
    input_filename = sys.argv[1]
    success = process_jotform_csv(input_filename)
    
    if success:
        print("Done.")
    else:
        sys.exit(1)
