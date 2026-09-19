# Hospital / Clinic Management System (Group 4)

A complete ASP.NET Core MVC (.NET 8) + ADO.NET + Microsoft SQL Server implementation based on the supplied Group 4 project documentation.

## Demo feedback implemented

1. **Appointment data consolidated**: appointment lists/details use joined patient + doctor + schedule data through `AppointmentRepository` and `AppointmentListItem`, avoiding fragmented display data.
2. **Sorting**: sortable list tables for Patients, Doctors, Appointments and Billing; prescription date ordering is also selectable.
3. **Pagination**: server-side SQL pagination (`OFFSET ... FETCH`) for Patients, Doctors, Appointments, Medical History, Prescriptions and Bills.
4. **Date filters**: appointment filtering by date range, billing filtering by bill date, plus a date-based Reports page.
5. **UI redesign**: responsive Bootstrap 5 layout, dashboard cards, filter panels, status badges, responsive tables and cleaner forms.

## Core modules

- Patient Registration / CRUD
- Doctor Management and deactivation
- Appointment Booking, rescheduling, status updates and doctor/time conflict checking
- Patient Medical History
- Prescription Management
- Billing and payment status updates
- Patient Search by ID, name or contact number
- Date-based summary Reports

## Setup

### 1. Requirements
- Visual Studio 2022 with ASP.NET and web development workload, or .NET 8 SDK
- Microsoft SQL Server / SQL Server Express / LocalDB

### 2. Create the database
Open SQL Server Management Studio and run:

- `sql/database.sql`
- Optional demo records: `sql/seed_demo.sql`

The schema creates database `HospitalClinicDB` and the six documented core tables.

### 3. Configure connection string
Edit `appsettings.json` if your SQL Server instance differs from LocalDB:

```json
"DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=HospitalClinicDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
```

For SQL Server Express, a common example is:

```text
Server=.\\SQLEXPRESS;Database=HospitalClinicDB;Trusted_Connection=True;TrustServerCertificate=True
```

### 4. Run

```bash
dotnet restore
dotnet run
```

Or open `HospitalClinicManagementSystem.csproj` in Visual Studio and press Run.

## Important implementation notes

- All user-supplied values are passed to SQL through parameters.
- Sort columns are selected from hard-coded allowlists rather than user-provided SQL identifiers.
- Appointment conflict checks ignore cancelled appointments and support rescheduling the current appointment.
- Bills are limited to completed appointments and one bill per appointment.
- Medical history allows at most one entry per linked appointment; unlinked history entries are still permitted.
- Patient deletion is blocked when related clinical records exist; this protects referential integrity.

## Project structure

- `Controllers/` - MVC request handling
- `Models/` - the six documented entities + paging model
- `ViewModels/` - consolidated appointment/billing/report display models
- `Repositories/` - parameterized ADO.NET data access
- `Views/` - Razor UI
- `wwwroot/css/` - custom responsive design
- `sql/` - database and demo data scripts
