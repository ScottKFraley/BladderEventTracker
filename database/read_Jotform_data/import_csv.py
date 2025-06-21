import csv
from datetime import datetime


def urgency_to_int(text):
    urgency_map = {
        "0 - No real urgency": 0,
        "1 - Somewhat urgent": 1,
        "2 - Pretty urgent": 2,
    }
    return urgency_map.get(text, 1)  # Default to 1 if not found


def leak_to_int(text):
    leak_map = {"1. Slight": 1, "2. Moderate": 2, "3. Heavy": 3}
    return leak_map.get(text, 1)  # Default to 1 if not found


def yesNo_to_bool(input_text):
    if input_text.lower() in ['yes', 'y', 'true', '1']:
        return 'true'
    return 'false'


def convert_file_to_sql():
    # The UUID for the user
    USER_ID = "8e3ddf21-4153-4838-abc6-47d553a5d905"

    # Open your input and output files
    with open("input.txt", "r") as infile, open("output.sql", "w") as outfile:
        # Create a tab-delimited reader
        reader = csv.DictReader(infile, delimiter="\t")

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
            if not first_row:
                outfile.write(",\n")
            else:
                first_row = False

            # Format the values
            values = f"""(
    TIMESTAMP WITH TIME ZONE '{row["EventDate"]}',
    {yesNo_to_bool(row["Accident"])},
    {yesNo_to_bool(row["ChangePadOrUnderware"])},
    {leak_to_int(row["LeakAmount"])},
    {urgency_to_int(row["Urgency"])},
    {yesNo_to_bool(row["AwokeFromSleep"])},
    {row["PainLevel"]},
    '{row["Notes"].replace("'", "''")}',
    '{USER_ID}'
)"""
            outfile.write(values)

        # End the statement
        outfile.write(";\n")


if __name__ == "__main__":
    convert_file_to_sql()
