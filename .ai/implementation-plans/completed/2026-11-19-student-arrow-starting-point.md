# Student arrow starting point

- Status: Completed
- Created and completed: 2026-11-19
- Context: Dashboard startup and lesson navigation

The startup query can choose the student from today's calendar while the dashboard uses that student's oldest loaded calendar occurrence as the arrow starting point. An occurrence in the previous month has no visible row, so the first Previous click can unexpectedly wrap to a later lesson.

## Stages

1. In `MainWindowViewModel`, prefer the current student's closest occurrence today in the displayed month, then their first occurrence in that month. Keep a chosen occurrence by its value across calendar refreshes. Arrows still step through calendar occurrences, including consecutive lessons for the same student, and the dropdown still switches students without initially selecting a lesson.
2. Test startup with an older occurrence outside the displayed month, a lesson today, and another student's prior lesson. Invoke Previous and verify that it selects that prior lesson. Verify existing same-student, month-crossing, and dropdown behaviour through the dashboard tests.

## Final user test

Open the Ledger on a day when the startup student has a lesson. Check that the nearest lesson today is highlighted. Click the left arrow once and check that the dashboard moves to the previous calendar lesson, selecting it and showing its student.

Post-completion user test: mandatory.

## Completion

- Startup prioritises the closest lesson today for the displayed student, then the student's first occurrence in the displayed month. The dashboard remaps the selected occurrence across refreshed navigation lists.
- A WPF regression test opens the main window with a prior-month occurrence in the calendar data. It verifies the highlighted lesson today and checks that the first Previous click selects the preceding student's lesson.
- The Debug solution build passed with zero warnings and errors. The direct xUnit runner passed six focused navigation tests and all 407 tests.
