import sys
import uuid
from datetime import datetime, timedelta


def urgency_to_int(text):
    try:
        return int(text.split(':')[1])
    except (IndexError, ValueError):
        return 1  # Default to 1 if not found or invalid


def parse_field(line, field_name):
    """Extract a specific field from the line"""
    for part in line.split(','):
        if part.strip().startswith(f"{field_name}:"):
            return part.strip().split(':')[1]
    return None


def parse_notes(line):
    """Extract notes from the line"""
    start = line.find('notes:"')
    if start == -1:
        return None
    
    # Find the closing quote
    end = line.find('"', start + 7)
    if end == -1:
        return line[start+7:]  # If no closing quote, return everything after notes:"
    
    return line[start+7:end]


def parse_sleeping(line):
    """Extract sleeping information from the line"""
    start = line.find('was sleeping')
    if start == -1:
        return False
        
    return True


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


def start_parsing_datafile(input_filename):
    try:
        USER_ID = "688E6E82-75F3-451F-8A0B-40176C70F7F8"
        
        with open(input_filename, "r") as infile, open("OneNote_data_for_input.sql", "w") as outfile:
            print("File opened successfully.")
            
            current_date = None
            rows = []
            batch_size = 1000
            
            for line in infile:
                line = line.strip()
                
                # Check if this is a date line
                if len(line.split()) == 2 and '-' in line.split()[0]:
                    current_date = line.split()[0]
                    continue
                
                # Skip empty lines
                if not line or line == ',':
                    continue
                
                # Parse time entry
                try:
                    time_part = line.split(',')[0].strip()
                    full_datetime = datetime.strptime(f"{current_date} {time_part}", "%Y-%m-%d %H:%M")
                except (ValueError, TypeError):
                    print(f"Skipping invalid line: {line}")
                    continue
                
                # Create SQL row
                pain_level = parse_field(line, 'pain')
                urgency = parse_field(line, 'urgency')
                notes = parse_notes(line)
                sleeping = parse_sleeping(line)
                
                # For notes, escape single quotes for SQL Server
                if notes:
                    escaped_notes = notes.replace("'", "''")
                    notes_value = f"'{escaped_notes}'"
                else:
                    notes_value = 'NULL'

                # Generate UUID for Id column
                record_id = str(uuid.uuid4()).upper()
                
                values = f"('{record_id}', " \
                        f"'{USER_ID}', " \
                        f"'{full_datetime.strftime('%Y-%m-%d %H:%M:%S.%f')[:-3]}', " \
                        f"0, " \
                        f"0, " \
                        f"0, " \
                        f"{urgency_to_int(urgency) if urgency else 1}, " \
                        f"{1 if sleeping else 0}, " \
                        f"{pain_level if pain_level else 0}, " \
                        f"{notes_value})"
                
                rows.append(values)
                
                # Write batch when we reach batch_size
                if len(rows) >= batch_size:
                    write_batch(outfile, rows)
                    rows = []
            
            # Write any remaining rows
            if rows:
                write_batch(outfile, rows)
            
            print("SQL file generated successfully.")
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
        print("Usage: python script.py <input_filename>")
        sys.exit(1)
    
    input_filename = sys.argv[1]
    success = start_parsing_datafile(input_filename)
    
    if success:
        print("Done.")
    else:
        sys.exit(1)
