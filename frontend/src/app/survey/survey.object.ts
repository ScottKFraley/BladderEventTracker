const surveyJson = {
    title: "Tracking Log Survey",
    elements: [
        { type: "text", name: "EventDate", title: "Event Date", inputType: "date", isRequired: true },
        { type: "boolean", name: "Accident", title: "Was there an accident?" },
        { type: "boolean", name: "ChangePadOrUnderware", title: "Did you have to changed your pad or underwear?" },
        { type: "rating", name: "LeakAmount", title: "Leak Amount (0-3)", rateMax: 3 },
        { type: "rating", name: "Urgency", title: "Urgency (0-4)", rateMax: 4 },
        { type: "boolean", name: "AwokeFromSleep", title: "Did this wake you from sleep?" },
        { type: "rating", name: "PainLevel", title: "Pain Level (0-10)", rateMax: 10 },
        { type: "comment", name: "Notes", title: "Notes (optional)", maxLength: 2000 },
    ]
};
