import sys
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


def start_parsing_datafile(input_filename):
    try:
        USER_ID = "8e3ddf21-4153-4838-abc6-47d553a5d905"
        
        with open(input_filename, "r") as infile, open("OneNote_data_for_input.psql", "w") as outfile:
            print("File opened successfully.")
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
            
            current_date = None
            first_row = True
            
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
                
                # Use a single-line f-string instead of multi-line
                # PostgreSQL strings, such as notes, must be surrounded by 
                # single (') not double (") quotes. -SKF
                
                # For notes, we need to:
                # 1. Escape any single quotes by doubling them for PostgreSQL
                # 2. Ensure the final string is wrapped in single quotes, not double quotes
                if notes:
                    # Escape single quotes for PostgreSQL
                    escaped_notes = notes.replace("'", "''")
                    # Wrap in single quotes
                    notes_value = f"'{escaped_notes}'"
                else:
                    notes_value = 'DEFAULT'

                values = f"(TIMESTAMP WITH TIME ZONE '{full_datetime.isoformat()}', " \
                        f"DEFAULT, " \
                        f"DEFAULT, " \
                        f"DEFAULT, " \
                        f"{urgency_to_int(urgency) if urgency else 'DEFAULT'}, " \
                        f"{sleeping}, " \
                        f"{pain_level if pain_level else 'DEFAULT'}, " \
                        f"{notes_value}, " \
                        f"'{USER_ID}')"  

                if not first_row:
                    outfile.write(", \n")
                else:
                    first_row = False
                
                outfile.write(values)
            
            # End the statement
            outfile.write(";\n")
            print("SQL file generated successfully.")
    
    except FileNotFoundError:
        print(f"Error: The file '{input_filename}' was not found.")
        sys.exit(1)
    except PermissionError:
        print(f"Error: Permission denied accessing '{input_filename}'")
        sys.exit(1)
    except Exception as e:
        print(f"An error occurred: {str(e)}")
        sys.exit(1)
        
# Main execution
if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Usage: python script.py <input_filename>")
        sys.exit(1)
    
    input_filename = sys.argv[1]
    start_parsing_datafile(input_filename)
    
    print("Done.")
