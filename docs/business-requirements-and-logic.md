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

### Lesson
    date/time
    student
    plan name

### Company
    name
    address
    phone
    email
    CVR: number / blank
    VAT register date: date/time / blank
    VAT status: not registered / registered
    invoice folder path

### Invoice
    invoice number (unique, sequential)
    date
    company info
    student info
    (list) lesson date/time + lesson plan name + lesson price (without VAT)
  
  AND (optional, left out if not specified)
    price adjustment
    price adjustment reason (mandatory if price was adjusted)

  AND (ONLY if the company is VAT registered AND we are past the VAT register date):
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

The Lessons table is updated with student lessons when the invoice is printed.

The Plan is an agreement between teacher and student and determines how many lessons there are per week and how much the student will pay for them. If the 'lessons per week' is 'by appointment', there is no fixed number.

Plan catalog entries have a lifecycle:
  * Draft: editable and deletable, but not assignable to students.
  * Active: immutable and assignable to students; the only editable field is `Retire on`, and the allowed catalog action is Retire.
  * Retired: immutable and not assignable to students; the only allowed catalog action is Reactivate.

Plans are managed independently from Students from the Plan menu. When an Active catalog Plan is assigned to a Student, the Plan fields are copied into that Student's plan period. After that assignment, catalog edits while Draft, publish/retire/reactivate actions, and future catalog cleanup must not mutate existing Student plan periods or issued invoices.

An Active Plan's `Retire on` date is equivalent to choosing Retire on that date. When the application starts, due Active Plans are moved to Retired so they no longer appear for new student assignment. Existing Student plan periods keep their copied Plan data until the Student is explicitly moved to another Active Plan.

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

Google Calendar is the repository of truth for all future lessons.
Google Calendar is also the repository of truth for past lessons *until* the invoice is printed.
Once the invoice is printed, the Lessons table is the repository of truth for past lessons.

Relevant Google Calendar entries are identified by their first line, which may be one of these:
  $ <student short name>
  $<student short name>

Google Calendar entries that are *NOT* identified as relevant by their first line are *ALWAYS* ignored.

The first line remains the ownership/matching marker. A completed uninvoiced lesson may additionally contain this later line in the Google Calendar entry:
  Status: Completed

If that later status line is absent, the lesson is treated as Planned.

When an Invoice is printed, it is added to the Invoice table and the Lessons table is updated with the Lessons noted on the Invoice.
The Lessons table is only ever updated when invoices are printed.

## Actions

### Application Startup

Upon startup the application displays a window containing the current student short name and a list of lessons for:
  * previous month
  * current month
  * next month

Each lesson in the list contains:
  - date/time
  - state: completed / planned
  - invoiced: yes / no
  - paid: yes / outstanding (only if invoiced)

Lesson state does not change automatically merely because the lesson date/time has passed. The user marks a Planned lesson as Completed manually.

The main application window does not display student prices or monthly payment amounts. Prices belong in invoice preparation/printing and later administration views, not on the everyday lesson screen where students may see the display.

The application also opens Google Calendar in a separate browser window on the screen.
(if we technically can open Google Calendar as a separate pane in the application, that may be preferable)

The application will automatically refresh the lesson list from Google Calendar as suitable.

There are Previous / Next buttons for the selected student, which will switch to the previous student and the next student respectively (by date/time of lesson).
There are also Previous / Next buttons for the month, to switch the display back/forth by a month.

The application contains a Student menu with these entries:
  * Add Student
  * Edit Student

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

Lessons that are completed or invoiced:yes CAN NOT be edited or deleted in the main lesson list.

The only allowed operation on a Completed lesson is to mark it back to Planned. This requires an explicit confirmation prompt. Once a Completed lesson has been marked back to Planned, and provided it is still invoiced:no, it can again be edited as a planned lesson.

The planned and invoiced:no entries in the lesson list may be edited directly in the main window:
  * date/time may be changed
  * lesson may be cancelled (deleted)
  * new lesson may be added

Adding a lesson creates a new empty planned row for the current student in the main lesson list. The user fills in the date/time directly in that row.

Lesson date editing uses a date selector. Lesson time editing provides a time selector while still allowing typed time input. Lesson cancellation uses an explicit delete action with a confirmation prompt.

Schedule changes to the lessons in the lesson list are saved directly in Google Calendar.
If the update of Google Calendar fails, the user is notified immediately so they can take corrective action.

### Write and Print an Invoice

Invoices are written by the press of a button in the main application window: "Write Invoice"

Before the invoice is written, the system will display a dialogue with student short names.
The dialogue will list all students with lessons that day, who have not yet had an invoice printed this month.
The dialogue will preselect the student who has a lesson at the current time/date.
If no student has a lesson at the current time/date, the dialogue does not preselect.
The dialogue will also allow the user to choose from all students in the database.

Once a student has been selected, the application will display the invoice on screen as a pre-filled, partially editable form.
The form will contain the proposed final invoice layout and information.

The Invoice will contain the Company information.
The Invoice will contain the Student information.
The Invoice will contain a list of lessons matching the information for that Student in Google Calendar.
The Invoice will contain VAT information, if currently relevant.
The Invoice will contain price information.
The Invoice language is: Danish

The user may edit the list of lessons.
The user may add a price adjustment and reason.
The Invoice VAT and price information will reflect user changes.

The user may Cancel the edit at any time.
If the user Cancels, the invoice is not saved and no data are updated in the system.

Once the invoice has been edited, the user may press the "Print Invoice" button on the Invoice form.
The "Print Invoice" button does the following:
  - Saves a PDF copy of the invoice in the company invoice folder path.
    -- note: if save fails, the following actions are *not* performed, and the user is notified of the failure.
  - Saves the Invoice data to the Invoice table.
  - Prints a hard copy of the Invoice (only if the printer is available)
  - Closes the Invoice form and return to the main view.
  - When saving an Invoice and returning to the main view, the displayed data in the main view are updated with any changes.
  
Once an Invoice has been *successfully* saved, it can no longer be edited.

## Limitations

### Version 1.0

  * VAT exemptions for individual students are outside the current scope.
  * No VAT reporting.
  * No payment due/terms.
  * No support for credit notes/correction invoices.
  * No integration with MobilePay/Bank for payment registration.
  * No tax reserve estimate.
  * No automated backup.
