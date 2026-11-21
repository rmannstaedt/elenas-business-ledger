# Tutorial: From lesson to paid invoice

[Open the illustrated tutorial (PDF)](tutorial.pdf).

In this walkthrough, you will create a lesson plan, schedule one lesson for Mira, and prepare an invoice. All names, addresses, and business details below are fictional. The Google account and calendar are the ones you set up for the trial.

Before starting, complete the [setup guide](setup.md). Use a dedicated **Ledger Trial** calendar and a separate invoice folder. If you already use the Ledger for business, follow the guide's separate Windows account instructions so the trial has its own database.

## 1. Set up the fictional studio

In **Application > Settings**, enter **Maple Key Studio** as the company name, **18 Example Lane** and **Example City** as the two address lines, and `studio@example.test` as the contact email. For **Payment instructions**, choose **8 dage fra fakturadato**. Leave VAT registration off and the banking and CVR fields blank. Confirm your trial **Invoice folder** and **Google calendar** ID, then choose **Save**.

## 2. Publish a lesson plan

1. Open **Plan > Manage Plans** and choose **Add Plan**.
2. Name the plan **Piano 60 min** and enter **320** in **Price without VAT**. Use **1** lesson per week, **60** minutes, and **1** student. Keep **Paid monthly in advance** and the default flexibility.
3. Set the **Effective date** to the first day of the month you will use for the lesson. Choose **Save Draft**, then **Publish draft** to make the plan assignable, then close the catalog.

## 3. Add Mira and assign the plan

1. Open **Student > Manage Students** and choose **Add student**.
2. Enter **Mira** as the short name and **Mira Vale** as the full name. Use **42 Sample Lane**, **Example City**, and `mira@example.test` for the fictional contact details. Choose **Standard** as the age bracket and keep **Active** checked.
3. Choose **Piano 60 min** under **Active plan**. Set **Starts on** to the first day of the lesson month or another day on or before the first lesson. Under **Schedule**, set one weekly slot, such as Wednesday at **16:00**.
4. Choose **Save**, then close the student windows. The main window should now show Mira as the selected student.

## 4. Schedule and complete one lesson

1. In Google Calendar, create a timed event titled exactly `$ Mira` on the **Ledger Trial** calendar. Pick a date in the month shown in the Ledger, on or after the plan and student start dates. The first line of the calendar event's title links the event to Mira's short name.
2. In the Ledger, choose **Refresh**. Mira's lesson should appear in **Lessons** with the assigned plan and a **Planned** state.
3. Click **Planned** in the lesson's **State** column to mark it **Completed**. In Google Calendar, refresh the event to see the completion marker in its title.

Once you have tried the one-lesson workflow, you can use **Add Lesson** to create an event from the Ledger or **Populate Calendar** to fill missing slots for the selected student's schedule in the displayed month. Calendar population skips exact matches for that student and invoice-locked slots. Check the calendar yourself for other overlaps.

## 5. Preview, issue, and record payment

1. With Mira selected and the lesson month displayed, choose **Write Invoice**. Select **Mira** if needed and choose **Continue**.
2. In the invoice preview, check the student, invoice date, included lesson, and total. With just this lesson, the total is **320 DKK** for the fictional studio. Choose **Cancel** to finish at the preview stage.
3. To try the rest of the workflow, choose **Save & Print Invoice** from the preview. The Ledger issues an invoice to your trial folder and requests printing. On a fresh Ledger database, an invoice for November 2026 would be named `Invoice 001 November 2026 - Mira Vale.pdf`. The actual number and month follow your database and selected billing period.
4. Under **Student Invoices**, find the issued invoice. Click **Outstanding** in the **Status** column to mark it **Paid**. The linked lesson also reflects the change.
5. Open **Economy > Overview** for the invoice year to see how the issued invoice contributes to the period totals. Use **Export CSV** to save the report.

Invoice emails and payment fields in this tutorial are sample record data. The Ledger tracks payment status from the action you take in the invoice list.
