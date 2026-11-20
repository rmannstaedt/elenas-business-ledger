# Populate the displayed month and prefill new lessons

- Status: Completed
- Created and completed: 2026-11-19
- Context: Main-window lesson toolbar usability

## Decisions

- `Populate Calendar` covers the first through last day of the displayed month. Use the student's assigned plan in effect on each occurrence date. Skip exact timed matches for that student in Google Calendar and matching invoice-locked slots in the local ledger.
- `Add Lesson` starts an editable draft with today's date in the current month and the first day of another displayed month. Use the first saved time for that weekday or the student's first saved slot. Use the effective assigned plan if it is selectable. Leave an unavailable default open for input.
- Save on the `Add Lesson` click when the defaults give the row a valid date and plan. The saved lesson remains editable and deletable. If the plan is unavailable, retain the draft and save it once a plan is selected. Time remains optional.

## Stages

1. Update Core calendar population and the App command. Keep the existing planned event and local ledger save path. Verify early-month creation, exact duplicate skipping, repeat invocation, and plan selection by occurrence date.
2. Expose the selected student's saved schedule and plan periods through the dashboard. Initialise the draft before attaching it to the grid and keep defaults scoped to the selected student. Verify that the `Add Lesson` click saves the intended date, time, and plan. Use the created event id for selection so the user can edit or delete the new lesson even if another lesson is at the same time.
3. Check both workflows and update the README, business requirements, and profile. Run the Debug solution build and tests. The previous calendar population plan remains the history of its original time window.

All stages can proceed within the requested toolbar changes.

## Final user test

Use a test student and a safe calendar with a plan that covers the whole displayed month. Leave one scheduled slot before today missing and keep another slot already recorded. Run `Populate Calendar` twice and confirm the missing slot appears once and the existing slot keeps its event. Click `Add Lesson` in the current month and confirm that one new lesson is saved with today's date, the saved time, and the assigned plan. Edit or delete that test lesson from the row.

Post-completion user test: mandatory.

## Completion

- The month population test checks an earlier missing date, a calendar match, an invoice-locked local lesson, and a repeat run that creates zero further events.
- The WPF test invokes `Add Lesson` once and verifies that it saves the scheduled time and assigned plan for the current date. It checks selection of the newly created event with another lesson at the same time.
- Debug solution build passed with zero warnings and errors. The direct xUnit runner passed 17 focused tests and all 406 tests. The tests used a fake calendar.
- Updated the README, profile, business rules, and handoff with the new toolbar behaviour.
