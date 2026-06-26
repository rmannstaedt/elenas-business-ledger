## Classes

### Student
    short name
    full name
    address
    phone
    email
    age bracket: young / standard
    birthdate (mandatory for age bracket 'young', otherwise optional)
    student plan (list: Plan with start/end dates for when the Plan is in effect for the student)
    schedule (weekday/time list)
    active: yes / no

### Plan
    name
    price (without VAT)
    lessons per week: 1 / 2 / by appointment
    duration: 30 / 60
    flexibility: none / move or cancel by agreement
    number of students: 1 / 2
    effective date: <date/time>
    payment terms: paid monthly in advance / paid per lesson

### Lesson
    date/time
    student
    plan identity
    plan name
    Google Calendar event id, when linked
    invoice number, when invoiced

### Company
    name
    address
    phone
    email
    CVR: number / blank
    VAT register date: date/time / blank
    VAT status: not registered / registered
    invoice folder path
    bank registration number, bank account number, MobilePay account, and invoice payment instructions selected from fixed terms or custom text

### Invoice
    invoice number (unique, sequential; displayed with at least three digits, e.g. 001)
    date
    period: the month/date range the invoice is being written for
    company info
    student info
    (list) lesson date/time + lesson plan identity + issued lesson plan name + lesson price (without VAT)
    grouped service lines by lesson plan and price
    bank registration/account number lines, MobilePay account line, and payment instructions
  
  AND (optional, left out if not specified)
    price adjustment
    price adjustment reason (mandatory if price was adjusted)

  AND (ONLY if the company is VAT registered AND the invoice period starts on or after the VAT register date):
    total price without VAT
    VAT percentage
    VAT amount
 
  AND:
    total price

### Google Calendar Entry
  student short name
  date/time
  lesson state marker, when completed
  
## Business Logic

The student schedule is the agreed weekday(s) and time(s) that the student will have their Lesson. This is an indication and individual Lessons may be cancelled or moved. Likewise, the student schedule may be edited for permanent alteration.

Lessons are stored locally before invoice issue. Google Calendar remains the operational schedule source for uninvoiced lessons, but local `Lessons` records are the durable ledger records used for billing and invoice locking.

The Plan is an agreement between teacher and student and determines how many lessons there are per week and how much the student will pay for them. If the 'lessons per week' is 'by appointment', there is no fixed number.

Plan catalog entries have a lifecycle:
  * Draft: editable and deletable, but not assignable to students. A Draft may be incomplete.
  * Active: immutable and assignable to students; the only editable fields are the display name and `Retire on`, and the allowed catalog action is Retire.
  * Retired: immutable and not assignable to students; the display name may still be renamed, and the only lifecycle action is Reactivate.

A Draft Plan starts with blank name, blank price, one lesson per week, 60 minute duration, one student, move/cancel flexibility, `Paid monthly in advance`, the first day of the current month as effective date, and no `Retire on` date. All fields except `Retire on` must be filled with valid values before the Draft can be published.

Plans are managed independently from Students from the Plan menu. When an Active catalog Plan is assigned to a Student, the catalog Plan identity is stored on that Student's plan period and the commercial Plan fields are copied into that Student's plan period. After that assignment, catalog edits while Draft, publish/retire/reactivate actions, name-only renames, and future catalog cleanup must not mutate existing Student plan period commercial terms or issued invoices. Name-only renames are tracked with old name, new name, and change date/time so invoice-time names can still be understood later.

An Active Plan's `Retire on` date is equivalent to choosing Retire on that date. When the application starts, due Active Plans are moved to Retired so they no longer appear for new student assignment. Existing Student plan periods keep their copied Plan data until the Student is explicitly moved to another Active Plan.

When adding another Plan from Manage Plans, changed Draft fields are saved before the new Draft is created. If the selected Active Plan has an unsaved `Retire on` change, the user is asked whether to save or discard that change before adding the new Draft.

Students may be moved to a different Active Plan by creating a new assigned student plan period.

If a new Plan is added to a Student, it takes effect at the Plan effective date/time.
If the new Plan effective date/time is in the future, the new Plan is not in effect until then.
Student plans may have an individual start date, in which case the will not take effect until then.
The old Plan is in effect until the new Plan takes effect.
There can only be one active Plan for a Student.
Old plans are retained for the Student, with information on when they started and ended.

If a Student leaves, the current Student Plan is listed with that effective end date and the Student is marked active:no.

When a Student starts, the Student Plan start date marks the first lesson.

The application will sync with Google Calendar.

Google Calendar is the repository of truth for scheduling details of uninvoiced lessons.
Once a lesson is invoiced, the local lesson record is locked to that invoice and later Google Calendar edits or deletions are ignored for billing and audit purposes.
Issued invoice lines remain immutable invoice-time snapshots for the printed invoice content.

Google authorization requests the `https://www.googleapis.com/auth/calendar.events.owned` OAuth scope. This lets the app read, create, move, complete, and delete events on Google calendars owned by the signed-in account, including lesson appointments the user creates manually in Google Calendar. The configured calendar should therefore be owned by that account; the app does not request broader all-calendar event access merely to inspect shared calendars.

Relevant Google Calendar entries are identified by their first line, which may be one of these:
  $ <student short name>
  $<student short name>

Google Calendar entries that are *NOT* identified as relevant by their first line are *ALWAYS* ignored.

The first line remains the ownership/matching marker. A completed uninvoiced lesson may additionally contain this later line in the Google Calendar entry:
  Status: Completed

If that later status line is absent, the lesson is treated as Planned.

When an Invoice is issued, it is added to the Invoice table with its issued invoice lines, and the included local Lessons are linked to that Invoice number. Those lesson links are the durable local record of which lessons were invoiced.

Issued Invoice lines store the catalog Plan identity for lineage/reporting and also store the Plan name as it appeared when the Invoice was issued. Invoice viewing and PDF output use the stored issued-time Plan name snapshot, not current catalog names and not a derived lookup through Plan name-change history.

## Actions

### Application Startup

Before the main window is shown, the application creates the app-data folder if needed and applies pending EF Core migrations to the local SQLite database. Startup then composes the local repositories, Google Calendar gateway, invoice PDF/printing services, and WPF view models through the App-layer bootstrapper. Schema changes must be represented as EF migrations, not ad hoc startup table/column mutation.

Upon startup the application displays a window containing the current student short name and a list of lessons for:
  * previous month
  * current month
  * next month

Each lesson in the list contains:
  - date/time
  - plan
  - state: completed / planned
  - invoiced: yes / no
  - paid: yes / outstanding (only if invoiced)

Lesson state does not change automatically merely because the lesson date/time has passed. The user marks a Planned lesson as Completed manually.

The main application window does not display student prices or monthly payment amounts. Prices belong in invoice preparation/printing and later administration views, not on the everyday lesson screen where students may see the display.

The application also opens Google Calendar in a separate browser window on the screen.
(if we technically can open Google Calendar as a separate pane in the application, that may be preferable)

The application will automatically refresh the lesson list from Google Calendar as suitable.

There are Previous / Next buttons for the selected student. These navigate a calendar-based occurrence list, not an alphabetical student-name list. Google Calendar lessons for active students in the loaded previous/current/next month window are ordered by lesson date/time and the same Student may therefore appear multiple times before other Students appear. The target Google Calendar lesson is selected in the lesson list so the reason for the selected Student is visible. If the target lesson is in another month, the displayed month changes to that target lesson's month. If there are no Google Calendar lessons in the loaded navigation window, navigation falls back to active Students' saved standard schedule day/time entries for the displayed month; those fallback entries are schedule hints, not actual calendar lessons. The selected Student name also has a dropdown menu listing other active Student short names alphabetically. Choosing a Student from that dropdown directly switches the selected Student and refreshes that Student's rows, but does not select a lesson.
There are also Previous / Next buttons for the month, to switch the display back/forth by a month.

The application contains a Student menu with this entry:
  * Manage Students

Manage Students lists all known Students, including inactive Students. Selecting a Student opens that Student in an embedded editor showing Student fields, Active flag, current assigned Plan snapshot, plan assignment selector, schedule fields, and issued Invoices. Add Student creates a new in-pane draft. Changes are explicit: Save persists the edited Student data and Cancel discards unsaved changes. Selecting an Active catalog Plan copies the Plan fields into the Student's plan period. Changing the Active flag, plan period dates, or schedule only persists when Save is pressed. Double-clicking an Invoice in the selected Student's invoice list opens it in the Invoice Viewer. Selecting a Student in Manage Students immediately refreshes the main window around that Student; closing Manage Students applies the final selected Student again.

The application contains a Plan menu with:
  * Manage Plans

Manage Plans lists Draft, Active, and Retired catalog Plans. Draft Plans can be edited, deleted, and published. Active Plans are read-only except for `Retire on`, and can be retired. Retired Plans are read-only and can be reactivated. Only Active Plans appear in the Student editor assignment selector.

The application window also displays a pane with Student Invoices.
Each Invoice in the list has a flag: Paid / Outstanding
Clicking on an 'Outstanding' flag toggles it to 'Paid'.
Clicking on a 'Paid' flag opens a dialog asking the User to confirm if the Invoice actually should be toggled to 'Unpaid'.
Double-clicking on an Invoice opens it in the Invoice Viewer.

The application also contains a button:
  * Write Invoice

The lesson toolbar contains a `Populate Calendar` action for the selected Student and displayed month. This action seeds Google Calendar immediately from the selected Student's saved standard schedule:
  * If the displayed month is the current month, the action creates scheduled lesson entries from today (inclusive) through the end of the month.
  * If the displayed month is a future month, the action creates scheduled lesson entries for the full month.
  * Past months cannot be seeded.
  * The action writes the resulting lesson entries to Google Calendar immediately; there is no separate save operation.
  * Exact duplicate lessons for the same Student at the same date/time are skipped.
  * Version 1 does not detect or handle other calendar conflicts. Conflicts must be resolved manually by the user in Google Calendar.

Lessons that are completed or invoiced:yes CAN NOT be edited or deleted in the main lesson list.

The only allowed operation on a Completed lesson is to mark it back to Planned. This requires an explicit confirmation prompt. Once a Completed lesson has been marked back to Planned, and provided it is still invoiced:no, it can again be edited as a planned lesson.

The planned and invoiced:no entries in the lesson list may be edited directly in the main window:
  * date/time may be changed
  * plan may be changed
  * lesson may be cancelled (deleted)
  * new lesson may be added

Adding a lesson creates a new empty planned row for the current student in the main lesson list. If the displayed month is not the current month, the new row starts on the first day of the displayed month. The user fills in the date/time directly in that row.

Lesson date editing uses a date selector. Lesson time editing provides a time selector from 08:00 through 20:00 in 15-minute increments while still allowing typed time input. Lesson cancellation uses an explicit delete action with a confirmation prompt.

Schedule changes to the lessons in the lesson list are saved directly in Google Calendar.
If the update of Google Calendar fails, the user is notified immediately so they can take corrective action.

### Write and Print an Invoice

Invoices are written by the press of a button in the main application window: "Write Invoice"

Before the invoice is written, the system will display a dialogue with student short names.
The dialogue will list all students with local uninvoiced lessons in the selected period. A student may appear even if another invoice already exists for that same month, provided later lessons remain uninvoiced.
When opened from the main window, the dialogue will preselect the currently displayed Student if that Student is still in the all-students list.
If the currently displayed Student is not available, the dialogue will preselect the Student who has a lesson at the current time/date.
If neither of those choices is available, the dialogue falls back to the first listed candidate/all-student entry so the visible selection remains explicit before Continue.
The dialogue will also allow the user to choose from all students in the database.

Once a student has been selected, the application will display the invoice on screen as a pre-filled, partially editable form.
The form will contain the proposed final invoice layout and information.

The Invoice will contain the Company information.
The Invoice will contain the Student information.
The Invoice will contain the period/month it is written for.
The Invoice will contain grouped service lines summarizing lessons by issued lesson plan and price.
The Invoice will contain a single clear list of local uninvoiced lessons selected for that Student.
If the invoice covers lessons under more than one Plan, the explanatory text lists the plan change with the cut-over date.
The Invoice will contain VAT information, if currently relevant.
VAT applicability is based on the invoice period start date, not the invoice issue/print date.
The Invoice will contain price information.
The Invoice will contain the configured bank registration/account number lines, MobilePay account line, and payment instructions, if any.
The Invoice must fit on one PDF page. The included lesson list is indented in the middle lesson area, stays in one column through 10 lessons, and uses two columns above 10 lessons so payment instructions, totals, and footer text are not pushed to a second page.
Payment instructions are configured as one of:

- `Betales ved modtagelse`
- `8 dage fra fakturadato`
- `30 dage fra fakturadato`
- custom text

The payment instructions and totals should sit near the bottom of the invoice page, above the footer text.
The Invoice language is: Danish

The user may edit the list of lessons.
The user may add a price adjustment and reason.
The Invoice VAT and price information will reflect user changes.

The user may Cancel the edit at any time.
If the user Cancels, the invoice is not saved and no data are updated in the system.

Once the invoice has been edited, the user may press the "Print Invoice" button on the Invoice form.
The "Print Invoice" button does the following:
  - Reserves the Invoice in the local database as an in-progress issue operation.
  - Saves a PDF copy of the invoice in the company invoice folder path through a staging, verification, and publish flow.
    -- note: the final PDF file name uses the invoice number, invoice period, and Student full name, e.g. `Invoice 001 Juni 2026 - Mira Vale.pdf`.
    -- note: if PDF staging, writing, verification, or publish fails before the final PDF is published, any staged PDF is discarded and the reserved Invoice number is released; the Invoice is not shown as issued, later actions are *not* performed, and the user is notified of the failure. Retrying must reuse that Invoice number rather than skipping to the next one.
  - Finalizes the Invoice data in the Invoice table as issued.
  - Sends a print request to the configured invoice printer, if one is configured and available.
    -- note: a print failure does not undo the issued Invoice or delete the PDF; the Invoice remains issued and can be reprinted from the Invoice Viewer.
  - Closes the Invoice form and returns to the main view only when the Invoice was issued and the print request was launched successfully.
  - Keeps the Invoice form open with an error message if the Invoice was issued but the print request failed.
  - When an Invoice is issued and returning to the main view, the displayed data in the main view are updated with any changes.
  
Once an Invoice has been *successfully* saved, it can no longer be edited.
Issued Invoices show payment and print status in the main/student invoice lists. The Invoice Viewer shows the stored PDF path, print status, and last print error when present. Reprinting an issued Invoice uses the stored PDF and existing Invoice number; it must not create a new Invoice number or change the issued Invoice content.

The invoice printer is selected from Application Settings during setup. If no invoice printer has been selected when printing or reprinting is first attempted, the app opens the printer selection dialog directly, lets the user choose a printer, and remembers that printer before continuing. The selected printer is remembered for subsequent invoice printing and can later be changed or cleared from Application Settings. When the app records an Invoice as Printed, that means the configured printer existed and Windows accepted the print request launch; the app does not verify physical paper output or Windows spooler completion.

## Limitations

### Version 1.0

  * VAT exemptions for individual students are outside the current scope.
  * No VAT reporting.
  * No automated payment reminders or due-date workflow.
  * No support for credit notes/correction invoices.
  * No integration with MobilePay/Bank for payment registration.
  * No tax reserve estimate.
  * No automated backup.
  * No calendar conflict detection for `Populate Calendar` beyond exact duplicate same-student same-date/time skipping.
