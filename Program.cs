using HospitalClinicManagementSystem.Data;
using HospitalClinicManagementSystem.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();
builder.Services.AddScoped<PatientRepository>();
builder.Services.AddScoped<DoctorRepository>();
builder.Services.AddScoped<AppointmentRepository>();
builder.Services.AddScoped<MedicalHistoryRepository>();
builder.Services.AddScoped<PrescriptionRepository>();
builder.Services.AddScoped<BillRepository>();
builder.Services.AddScoped<DashboardRepository>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
