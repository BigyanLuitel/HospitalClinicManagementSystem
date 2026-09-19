USE HospitalClinicDB;
GO

INSERT INTO Patients (FirstName,LastName,DateOfBirth,Gender,ContactNumber,Email,Address,BloodGroup)
VALUES
('Aarav','Shrestha','1998-04-12','Male','9800000001','aarav@example.com','Kathmandu','O+'),
('Sita','Karki','2001-09-03','Female','9800000002','sita@example.com','Lalitpur','A+'),
('Nabin','Gurung','1987-02-20','Male','9800000003',NULL,'Bhaktapur','B+');
GO

INSERT INTO Doctors (FullName,Specialization,ContactNumber,Email,AvailableDays,ConsultationFee,Status)
VALUES
('Dr. Maya Adhikari','General Medicine','9811000001','maya@clinic.test','Sun-Fri',800,'Active'),
('Dr. Rohan Joshi','Cardiology','9811000002','rohan@clinic.test','Mon-Fri',1500,'Active'),
('Dr. Anisha Rai','Pediatrics','9811000003','anisha@clinic.test','Sun-Thu',1000,'Active');
GO

INSERT INTO Appointments (PatientID,DoctorID,AppointmentDate,AppointmentTime,Reason,Status)
VALUES
(1,1,CAST(GETDATE() AS DATE),'09:30','General check-up','Scheduled'),
(2,2,DATEADD(DAY,1,CAST(GETDATE() AS DATE)),'11:00','Follow-up consultation','Scheduled'),
(3,1,DATEADD(DAY,-2,CAST(GETDATE() AS DATE)),'14:00','Fever and fatigue','Completed');
GO

INSERT INTO MedicalHistory (PatientID,DoctorID,AppointmentID,VisitDate,Diagnosis,Notes)
VALUES (3,1,3,DATEADD(DAY,-2,CAST(GETDATE() AS DATE)),'Viral fever','Rest, hydration and follow-up if symptoms persist.');
GO

INSERT INTO Prescriptions (AppointmentID,PatientID,DoctorID,Medicines,Dosage)
VALUES (3,3,1,'Paracetamol 500 mg','One tablet after food, up to three times daily for 3 days.');
GO

INSERT INTO Bills (PatientID,AppointmentID,Amount,PaymentStatus,PaymentMethod)
VALUES (3,3,800,'Paid','Cash');
GO
