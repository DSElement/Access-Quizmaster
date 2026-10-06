# Product FAQ — Acme CRM

Acme CRM supports bulk imports via CSV, REST API integration, and real-time dashboards out of the box. For enterprise licensing or custom integrations, contact sales@acme.com.

## What information does Acme CRM manage?

Acme CRM organises customer contacts, company accounts, sales opportunities, and activity history. Teams can assign record owners and track an opportunity through their configured pipeline stages. Role permissions determine which records an employee can view or edit. A dashboard does not grant access to records that the employee is otherwise unable to see.

Before importing customer information, agree the record ownership and required fields with the CRM administrator. Use the organisation's approved naming conventions so sales and support teams can identify the same customer consistently.

## How do bulk CSV imports work?

Use the import wizard to upload a UTF-8 CSV file, map columns to CRM fields, and preview validation results. A contact import must include an email address or another configured unique identifier. Correct invalid rows before confirming the import. Begin with a small sample file when setting up a new mapping.

An import job accepts up to 5,000 rows and a 10 MB file. Split larger datasets into several files. The job summary shows imported, updated, and rejected rows, with an error report for rejected entries. Download that report and correct the affected rows rather than uploading the entire file again without checking the result.

## How are duplicate records handled?

The import wizard can match existing contacts using the selected unique identifier. Choose whether matching records should be updated or skipped before starting the job. An email address with an accidental space or spelling error may fail to match the intended record, so normalise your data first.

Imports do not automatically merge two unrelated customer accounts. An administrator should review suspected duplicates and confirm ownership before merging records. Keep an export of the original data when making a large cleanup change.

## How can another application use the REST API?

The REST API supports reading and updating CRM records using an integration credential created by a CRM administrator. Grant only the permissions required by the integration. Store credentials in the application's secret configuration rather than in source code or a CSV file. Rotate the credential if it is exposed.

The standard API limit is 100 requests per minute per integration credential. A rate-limited request returns HTTP 429. The integration should wait for the indicated retry interval and avoid retrying continuously. Use pagination when retrieving large record lists, and record request identifiers when investigating an error.

## Why does a dashboard show different figures?

Real-time dashboards reflect saved CRM changes, subject to the user's filters and record permissions. Check the selected date range, team, pipeline stage, and owner before comparing two dashboards. Unsaved edits are not included. An administrator can check whether the users have different permissions or report definitions.

## Who handles licensing and custom integrations?

Contact sales@acme.com for enterprise licensing or a custom integration proposal. Include the expected user count, required systems, and business workflow. For an existing tenant's access or import problem, contact the CRM administrator first; internal device or VPN issues belong with the IT helpdesk.
