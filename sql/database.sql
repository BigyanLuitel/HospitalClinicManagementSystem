/* Hospital / Clinic Management System - SQL Server schema
   Based on the six core entities from the project documentation. */

IF DB_ID(N'HospitalClinicDB') IS NULL
BEGIN
    CREATE DATABASE HospitalClinicDB;
END
GO

USE HospitalClinicDB;
GO

IF OBJECT_ID('dbo.Bills','U') IS NOT NULL DROP TABLE dbo.Bills;
IF OBJECT_ID('dbo.Prescriptions','U') IS NOT NULL DROP TABLE dbo.Prescriptions;
IF OBJECT_ID('dbo.MedicalHistory','U') IS NOT NULL DROP TABLE dbo.MedicalHistory;
IF OBJECT_ID('dbo.Appointments','U') IS NOT NULL DROP TABLE dbo.Appointments;
IF OBJECT_ID('dbo.Doctors','U') IS NOT NULL DROP TABLE dbo.Doctors;
IF OBJECT_ID('dbo.Patients','U') IS NOT NULL DROP TABLE dbo.Patients;
GO

CREATE TABLE dbo.Patients (
    PatientID        INT IDENTITY(1,1) PRIMARY KEY,
    FirstName        NVARCHAR(50)  NOT NULL,
    LastName         NVARCHAR(50)  NOT NULL,
    DateOfBirth      DATE          NOT NULL,
    Gender           NVARCHAR(10)  NOT NULL,
    ContactNumber    NVARCHAR(15)  NOT NULL,
    Email            NVARCHAR(100) NULL,
    Address          NVARCHAR(200) NULL,
    BloodGroup       NVARCHAR(5)   NULL,
    RegistrationDate DATETIME      NOT NULL CONSTRAINT DF_Patients_RegistrationDate DEFAULT GETDATE(),
    CONSTRAINT CK_Patients_Gender CHECK (Gender IN ('Male','Female','Other'))
);
GO

CREATE TABLE dbo.Doctors (
    DoctorID        INT IDENTITY(1,1) PRIMARY KEY,
    FullName        NVARCHAR(100) NOT NULL,
    Specialization  NVARCHAR(100) NOT NULL,
    ContactNumber   NVARCHAR(15)  NOT NULL,
    Email           NVARCHAR(100) NULL,
    AvailableDays   NVARCHAR(50)  NULL,
    ConsultationFee DECIMAL(10,2) NOT NULL,
    Status           NVARCHAR(20) NOT NULL CONSTRAINT DF_Doctors_Status DEFAULT 'Active',
    CONSTRAINT CK_Doctors_Status CHECK (Status IN ('Active','Inactive')),
    CONSTRAINT CK_Doctors_Fee CHECK (ConsultationFee >= 0)
);
GO

CREATE TABLE dbo.Appointments (
    AppointmentID   INT IDENTITY(1,1) PRIMARY KEY,
    PatientID       INT          NOT NULL,
    DoctorID        INT          NOT NULL,
    AppointmentDate DATE         NOT NULL,
    AppointmentTime TIME(0)      NOT NULL,
    Reason           NVARCHAR(200) NULL,
    Status           NVARCHAR(20) NOT NULL CONSTRAINT DF_Appointments_Status DEFAULT 'Scheduled',
    CreatedDate      DATETIME     NOT NULL CONSTRAINT DF_Appointments_CreatedDate DEFAULT GETDATE(),
    CONSTRAINT FK_Appointments_Patients FOREIGN KEY (PatientID) REFERENCES dbo.Patients(PatientID),
    CONSTRAINT FK_Appointments_Doctors FOREIGN KEY (DoctorID) REFERENCES dbo.Doctors(DoctorID),
    CONSTRAINT CK_Appointments_Status CHECK (Status IN ('Scheduled','Completed','Cancelled'))
);
GO

CREATE TABLE dbo.MedicalHistory (
    HistoryID     INT IDENTITY(1,1) PRIMARY KEY,
    PatientID     INT           NOT NULL,
    DoctorID      INT           NOT NULL,
    AppointmentID INT           NULL,
    VisitDate      DATE          NOT NULL,
    Diagnosis      NVARCHAR(300) NULL,
    Notes          NVARCHAR(500) NULL,
    CONSTRAINT FK_MedicalHistory_Patients FOREIGN KEY (PatientID) REFERENCES dbo.Patients(PatientID),
    CONSTRAINT FK_MedicalHistory_Doctors FOREIGN KEY (DoctorID) REFERENCES dbo.Doctors(DoctorID),
    CONSTRAINT FK_MedicalHistory_Appointments FOREIGN KEY (AppointmentID) REFERENCES dbo.Appointments(AppointmentID)
);
GO

CREATE TABLE dbo.Prescriptions (
    PrescriptionID INT IDENTITY(1,1) PRIMARY KEY,
    AppointmentID  INT           NOT NULL,
    PatientID      INT           NOT NULL,
    DoctorID       INT           NOT NULL,
    Medicines      NVARCHAR(500) NOT NULL,
    Dosage         NVARCHAR(200) NULL,
    DateIssued     DATETIME      NOT NULL CONSTRAINT DF_Prescriptions_DateIssued DEFAULT GETDATE(),
    CONSTRAINT FK_Prescriptions_Appointments FOREIGN KEY (AppointmentID) REFERENCES dbo.Appointments(AppointmentID),
    CONSTRAINT FK_Prescriptions_Patients FOREIGN KEY (PatientID) REFERENCES dbo.Patients(PatientID),
    CONSTRAINT FK_Prescriptions_Doctors FOREIGN KEY (DoctorID) REFERENCES dbo.Doctors(DoctorID)
);
GO

CREATE TABLE dbo.Bills (
    BillID         INT IDENTITY(1,1) PRIMARY KEY,
    PatientID      INT           NOT NULL,
    AppointmentID  INT           NOT NULL,
    Amount         DECIMAL(10,2) NOT NULL,
    PaymentStatus  NVARCHAR(20)  NOT NULL CONSTRAINT DF_Bills_PaymentStatus DEFAULT 'Unpaid',
    PaymentMethod  NVARCHAR(30)  NULL,
    BillDate       DATETIME      NOT NULL CONSTRAINT DF_Bills_BillDate DEFAULT GETDATE(),
    CONSTRAINT FK_Bills_Patients FOREIGN KEY (PatientID) REFERENCES dbo.Patients(PatientID),
    CONSTRAINT FK_Bills_Appointments FOREIGN KEY (AppointmentID) REFERENCES dbo.Appointments(AppointmentID),
    CONSTRAINT CK_Bills_Amount CHECK (Amount > 0),
    CONSTRAINT CK_Bills_Status CHECK (PaymentStatus IN ('Paid','Unpaid','Partially Paid')),
    CONSTRAINT CK_Bills_Method CHECK (PaymentMethod IS NULL OR PaymentMethod IN ('Cash','Card','Insurance')),
    CONSTRAINT UQ_Bills_Appointment UNIQUE (AppointmentID)
);
GO

/* One history entry per appointment when an appointment is linked. */
CREATE UNIQUE INDEX UX_MedicalHistory_Appointment
ON dbo.MedicalHistory(AppointmentID)
WHERE AppointmentID IS NOT NULL;
GO

/* Helpful indexes for feedback items: date filtering, sorting, search and pagination. */
CREATE INDEX IX_Appointments_Date_Doctor ON dbo.Appointments(AppointmentDate, DoctorID, AppointmentTime) INCLUDE (PatientID, Status, Reason);
CREATE INDEX IX_Appointments_Status_Date ON dbo.Appointments(Status, AppointmentDate);
CREATE INDEX IX_Patients_Name ON dbo.Patients(FirstName, LastName) INCLUDE (ContactNumber, RegistrationDate);
CREATE INDEX IX_Doctors_Specialization ON dbo.Doctors(Specialization, Status) INCLUDE (FullName, ConsultationFee);
CREATE INDEX IX_Bills_Date_Status ON dbo.Bills(BillDate, PaymentStatus) INCLUDE (PatientID, Amount);
CREATE INDEX IX_Prescriptions_Patient_Date ON dbo.Prescriptions(PatientID, DateIssued);
CREATE INDEX IX_MedicalHistory_Patient_VisitDate ON dbo.MedicalHistory(PatientID, VisitDate);
GO
