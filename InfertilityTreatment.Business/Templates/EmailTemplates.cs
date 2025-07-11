using InfertilityTreatment.Entity.DTOs.Appointments;
using InfertilityTreatment.Entity.Entities;
using System.Text;

namespace InfertilityTreatment.Business.Templates
{
    public static class EmailTemplates
    {
        public static string WelcomeEmail(string customerName)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Welcome to Infertility Treatment System</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; }}
        .footer {{ background-color: #f4f4f4; padding: 10px; text-align: center; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>Welcome to Infertility Treatment System</h1>
    </div>
    <div class='content'>
        <h2>Dear {customerName},</h2>
        <p>Welcome to our Infertility Treatment System! We're excited to have you join our community.</p>
        <p>Your account has been successfully created and you can now:</p>
        <ul>
            <li>Schedule appointments with our specialists</li>
            <li>Track your treatment progress</li>
            <li>Access test results</li>
            <li>Communicate with your healthcare team</li>
        </ul>
        <p>If you have any questions, please don't hesitate to contact our support team.</p>
        <p>Best regards,<br>Infertility Treatment Team</p>
    </div>
    <div class='footer'>
        <p>&copy; 2025 Infertility Treatment System. All rights reserved.</p>
    </div>
</body>
</html>";
        }


        public static string AppointmentConfirmation(AppointmentResponseDto appointment, string customerName, string doctorName)
        {
            var notes = !string.IsNullOrEmpty(appointment.Notes)
                ? $"<p><strong>Notes:</strong> {appointment.Notes}</p>"
                : "";

            var template = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Appointment Confirmation</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .header {{ background-color: #2196F3; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; }}
        .appointment-details {{ background-color: #f9f9f9; padding: 15px; border-radius: 5px; margin: 15px 0; }}
        .footer {{ background-color: #f4f4f4; padding: 10px; text-align: center; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>Appointment Confirmation</h1>
    </div>
    <div class='content'>
        <h2>Dear {customerName},</h2>
        <p>Your appointment has been successfully scheduled!</p>
        <div class='appointment-details'>
            <h3>Appointment Details:</h3>
            <p><strong>Type:</strong> {appointment.AppointmentType}</p>
            <p><strong>Doctor:</strong> {doctorName}</p>
            <p><strong>Date:</strong> {appointment.ScheduledDateTime:MMMM dd, yyyy}</p>
            <p><strong>Time:</strong> {appointment.ScheduledDateTime:HH:mm}</p>
            <p><strong>Status:</strong> {appointment.Status}</p>
            {notes}
        </div>
        <p>Please arrive 15 minutes before your scheduled time.</p>
        <p>If you need to reschedule or cancel, please contact us at least 24 hours in advance.</p>
        <p>Best regards,<br>Infertility Treatment Team</p>
    </div>
    <div class='footer'>
        <p>&copy; 2025 Infertility Treatment System. All rights reserved.</p>
    </div>
</body>
</html>";

            return template;
        }



        public static string AppointmentReminder(AppointmentResponseDto appointment, string customerName, string doctorName)
        {
            var template = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Appointment Reminder</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .header {{ background-color: #FF9800; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; }}
        .appointment-details {{ background-color: #fff3e0; padding: 15px; border-radius: 5px; margin: 15px 0; border-left: 4px solid #FF9800; }}
        .footer {{ background-color: #f4f4f4; padding: 10px; text-align: center; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>Appointment Reminder</h1>
    </div>
    <div class='content'>
        <h2>Dear {customerName},</h2>
        <p>This is a friendly reminder about your upcoming appointment:</p>
        <div class='appointment-details'>
            <h3>Appointment Details:</h3>
            <p><strong>Type:</strong> {appointment.AppointmentType}</p>
            <p><strong>Doctor:</strong> {doctorName}</p>
            <p><strong>Date:</strong> {appointment.ScheduledDateTime:MMMM dd, yyyy}</p>
            <p><strong>Time:</strong> {appointment.ScheduledDateTime:HH:mm}</p>
        </div>
        <p>Please remember to:</p>
        <ul>
            <li>Arrive 15 minutes early</li>
            <li>Bring any required documents</li>
            <li>Take any prescribed medications as directed</li>
        </ul>
        <p>If you need to reschedule, please contact us immediately.</p>
        <p>Best regards,<br>Infertility Treatment Team</p>
    </div>
    <div class='footer'>
        <p>&copy; 2025 Infertility Treatment System. All rights reserved.</p>
    </div>
</body>
</html>";

            return template;
        }


        public static string TreatmentPhaseUpdate(TreatmentPhase phase, string customerName)
        {
            var description = !string.IsNullOrEmpty(phase.Instructions)
                ? $"<p><strong>Instructions:</strong> {phase.Instructions}</p>"
                : "";

            var template = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Treatment Phase Update</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .header {{ background-color: #9C27B0; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; }}
        .phase-details {{ background-color: #f3e5f5; padding: 15px; border-radius: 5px; margin: 15px 0; }}
        .footer {{ background-color: #f4f4f4; padding: 10px; text-align: center; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>Treatment Phase Update</h1>
    </div>
    <div class='content'>
        <h2>Dear {customerName},</h2>
        <p>We have an update regarding your treatment phase:</p>
        <div class='phase-details'>
            <h3>Phase Information:</h3>
            <p><strong>Phase:</strong> {phase.PhaseName}</p>
            <p><strong>Status:</strong> {phase.Status}</p>
            <p><strong>Start Date:</strong> {(phase.StartDate?.ToString("MMMM dd, yyyy") ?? "TBD")}</p>
            <p><strong>Expected End Date:</strong> {(phase.EndDate?.ToString("MMMM dd, yyyy") ?? "TBD")}</p>
            {description}
        </div>
        <p>Please follow your treatment plan and don't hesitate to contact us if you have any questions.</p>
        <p>Best regards,<br>Infertility Treatment Team</p>
    </div>
    <div class='footer'>
        <p>&copy; 2025 Infertility Treatment System. All rights reserved.</p>
    </div>
</body>
</html>";

            return template;
        }


        public static string TestResultNotification(TestResult result, string customerName)
        {
            var notes = !string.IsNullOrEmpty(result.DoctorNotes)
                ? $"<p><strong>Notes:</strong> {result.DoctorNotes}</p>"
                : "";

            var template = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Test Result Available</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .header {{ background-color: #00BCD4; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; }}
        .result-details {{ background-color: #e0f2f1; padding: 15px; border-radius: 5px; margin: 15px 0; }}
        .footer {{ background-color: #f4f4f4; padding: 10px; text-align: center; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>Test Result Available</h1>
    </div>
    <div class='content'>
        <h2>Dear {customerName},</h2>
        <p>Your test results are now available for review:</p>
        <div class='result-details'>
            <h3>Test Information:</h3>
            <p><strong>Test Type:</strong> {result.TestType}</p>
            <p><strong>Test Date:</strong> {result.TestDate:MMMM dd, yyyy}</p>
            <p><strong>Status:</strong> {result.Status}</p>
            {notes}
        </div>
        <p>Please log into your account to view the complete results or schedule a follow-up appointment with your doctor to discuss the findings.</p>
        <p>Best regards,<br>Infertility Treatment Team</p>
    </div>
    <div class='footer'>
        <p>&copy; 2025 Infertility Treatment System. All rights reserved.</p>
    </div>
</body>
</html>";

            return template;
        }


        


        public static string BulkNotificationTemplate(string title, string message, string customerName)
        {
            var template = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>{title}</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .header {{ background-color: #607D8B; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; }}
        .message-content {{ background-color: #f5f5f5; padding: 15px; border-radius: 5px; margin: 15px 0; }}
        .footer {{ background-color: #f4f4f4; padding: 10px; text-align: center; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>{title}</h1>
    </div>
    <div class='content'>
        <h2>Dear {customerName},</h2>
        <div class='message-content'>
            {message}
        </div>
        <p>Best regards,<br>Infertility Treatment Team</p>
    </div>
    <div class='footer'>
        <p>&copy; 2025 Infertility Treatment System. All rights reserved.</p>
    </div>
</body>
</html>";

            return template;
        }

    }
}
