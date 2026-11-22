# How to set up Elena's Business Ledger

You can use the Windows x64 installer or run the application from source. For a first run, use a dedicated Google calendar and a separate invoice folder. If this Windows account already has production Ledger data, use a separate Windows account for the tutorial. The Ledger uses a database per Windows user, so changing the calendar or invoice folder alone still uses the same student and invoice records.

## Install or run the application

**Windows installer.** Close any running copy of the Ledger and run `ElenasBusinessLedger-1.4.2-Setup.exe` from your normal Windows account. The installer includes the .NET runtime and installs per user. Open **Elena's Business Ledger** from the Start menu, then check **Application > About Elena's Business Ledger** for version **1.4.2**. The installer is unsigned, so Windows may show a SmartScreen prompt.

If you have an earlier version, run this installer over the existing installation. The installer uses the existing installation identity and replaces application files in place. The database, Settings, and Google authorization stay in the existing app data folder. [Inno Setup upgrade behavior](https://jrsoftware.org/inno-setup/faq)

**Source build.** On Windows, install the .NET 11 SDK. Open `ElenasBusinessLedger.sln` in Visual Studio 18.12 or later with the .NET desktop development workload and select `ElenasBusinessLedger.App` as the startup project. You can also start it from the repository root in PowerShell.

```powershell
dotnet run --project .\src\ElenasBusinessLedger.App\ElenasBusinessLedger.App.csproj
```

A source build needs a Desktop OAuth client JSON file for calendar access. An installer with a bundled client can use it if the OAuth project's audience allows your Google account. If that client is missing or your account is outside its audience, follow [Set up a Google OAuth client](#set-up-a-google-oauth-client).

## Configure company and invoices

1. Create a writable folder for issued invoices. For a trial, create a folder such as **Ledger Trial Invoices** in Documents.
2. Open **Application > Settings**. Under **Company**, enter the name and contact details that should appear on invoices. For a trial, use the fictional details in the [tutorial](tutorial.md#1-set-up-the-fictional-studio).
3. Under **Invoicing**, choose the **Invoice folder** and payment instructions. Select an **Invoice printer** if you want to print. When printing or reprinting for the first time with a blank printer setting, the Ledger asks you to select one and remembers it. Windows needs a PDF application registered to handle the `printto` shell action for the selected printer.
4. Enter bank or MobilePay details if you want them printed on invoices. Configure **VAT registered**, **VAT registration date**, and **CVR** to match your business. Leave these fields blank or off for the fictional studio in the tutorial.
5. Keep Settings open for the calendar steps below, or choose **Save** to keep the company and invoice settings and return later.

## Connect Google Calendar

1. In Google Calendar in a browser, sign in to the account that should own the lesson calendar. Next to **Other calendars**, select **+ > Create new calendar**. For the tutorial, name it **Ledger Trial** and choose **Create calendar**. [Google Calendar Help: Create a new calendar](https://support.google.com/calendar/answer/37095?hl=en)
2. Under **My calendars**, open the calendar's menu and choose **Settings and sharing**. In **Integrate calendar**, copy the **Calendar ID**. The ID and the calendar's display name are different fields. [Google Calendar Help: Find a calendar ID](https://support.google.com/calendar/answer/44105?hl=en)
3. In **Application > Settings**, paste the ID into **Google calendar** and enter the owning account's real email in **Google account email**. The default value `primary` points to the signed-in account's main calendar. For a trial, confirm that the dedicated calendar ID appears in the field before connecting.
4. Choose **Connect Google Calendar**. If the browser prompts for consent, select the same Google account and authorize the requested access. The Ledger verifies the connection by reading events for the current month. When Settings shows that the connection succeeded, choose **Save** to make the selected calendar active.
5. On the main window, choose **Refresh**. To check the connection with a real lesson record, continue with the [tutorial](tutorial.md). A new calendar starts empty.

The Ledger uses the configured calendar ID for lesson reads and writes. Google authorizes the app with the `https://www.googleapis.com/auth/calendar.events.owned` scope, which covers events on calendars the signed-in account owns. The app can create, update, and delete lesson events on its configured calendar. An owned calendar is required for this setup. [Google Calendar API scopes](https://developers.google.com/workspace/calendar/api/auth)

## Set up a Google OAuth client

Use this section for an unconfigured build or to authorize through a Google Cloud project you control.

1. Create or select a project in the Google Cloud console. Under **APIs & Services**, enable the **Google Calendar API** for that project. [Enable Google Workspace APIs](https://developers.google.com/workspace/guides/enable-apis)
2. Open **Google Auth Platform** and choose **Get started** if the project is new. Complete the consent setup with an app name, a support email, and a project contact address. For a personal Google account, choose **External** under **Audience**. While the app is in **Testing**, add the Google account that owns the lesson calendar under **Test users**. [Configure the OAuth consent screen](https://developers.google.com/workspace/guides/configure-oauth-consent)
3. In **Data Access > Add or Remove Scopes**, add the scope `https://www.googleapis.com/auth/calendar.events.owned`. [Google Calendar API scopes](https://developers.google.com/workspace/calendar/api/auth)
4. Under **Clients**, choose **Create client**, select **Desktop app**, enter a name, and create the client. Download its JSON configuration. [Create a desktop OAuth client](https://developers.google.com/workspace/guides/create-credentials#desktop-app)
5. Save the downloaded JSON as `google-oauth-client.json` in the applicable location.

   | Setup | File location |
   | --- | --- |
   | Source build | `src\ElenasBusinessLedger.App\Configuration\google-oauth-client.json` relative to the repository root. Create the `Configuration` folder if needed. The build copies the file into its output. |
   | Default per-user installation | `%LOCALAPPDATA%\Programs\MySmallBusinessLedger\Configuration\google-oauth-client.json`. For a custom install path, use the `Configuration` folder beside the installed launcher. |

6. Keep the downloaded file local. The source path is ignored by Git. Rebuild and restart a source build, or restart the installed app after placing the file. If you replaced an OAuth client after signing in, choose **Forget Google sign-in**, then follow [Connect Google Calendar](#connect-google-calendar) again.

## Troubleshooting

| What you see | What to check |
| --- | --- |
| Google sign-in app configuration is missing or invalid | Check the filename and folder. Use the JSON downloaded for a **Desktop app** OAuth client. Rebuild if running from source. |
| Google refuses sign-in or says access is blocked | Check the active Cloud project and its audience. For an external app in Testing, add the signing-in account to **Test users**. If a bundled client is restricted to other accounts, configure a client in your own project. |
| The calendar worked earlier and now asks to reconnect | In Settings, use **Connect Google Calendar**. If the app keeps picking up an old authorization, choose **Forget Google sign-in** first. Authorization for an external app in Testing expires after seven days. [Google OAuth audience and testing](https://support.google.com/cloud/answer/15549945?hl=en) |
| The connection fails or the calendar is empty | Verify the owning account, selected calendar ID, enabled Calendar API, and displayed month. For a manually added lesson, use a title such as `$ Mira` and confirm that `Mira` matches an active student's short name. |
| The Ledger reports a pending invoice | Click **Pending** in that student's invoice list or open **Invoice > Resume Invoice** from the main window. If you're viewing another student, the notice above the list identifies the pending invoice. Review the saved student, number, and total. Choose **Resume saved invoice** to finish issuing it. **Discard reservation** is available only before the PDF publication checkpoint. **Write Invoice** becomes available again when all pending reservations have been resolved. |
| **Review needed** appears on a lesson | Open the lesson's review action. Confirm the result against the same Google calendar before approving removal if the appointment is missing. The lesson is held out of billing until you resolve the review. |
| **Plan needed** appears on a lesson | Choose an active plan from that lesson's plan selector. The Ledger then links the appointment and can include it in billing when it meets the other eligibility rules. |
| The PDF exists and the print request failed | The invoice is already issued. Check the configured printer and Windows PDF association, then open the issued invoice and use **Reprint**. |

Deleting an appointment outside the Ledger leaves its local billing row in place. Review the selected lessons in the invoice preview before issuing.

## Where the Ledger stores data

**Application > Settings** shows the database location and app data folder. By default, `%APPDATA%\visualtech.dk\MySmallBusinessLedger` contains the SQLite database `ledger.db`, Windows user-protected Google authorization files under `GoogleTokens`, and saved window and grid layouts. Invoice PDFs are stored separately in the **Invoice folder** you selected. Installer upgrades and uninstall leave those data locations in place. To back up the ledger, close the application and copy the app data folder and the invoice folder.

Starting with version 1.4.0, the Ledger creates a verified SQLite backup in the app data folder under `Backups` before applying a schema upgrade. This backup covers the database. Continue to keep a copy of the separate invoice folder.
