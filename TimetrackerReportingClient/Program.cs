using Newtonsoft.Json;
using TimetrackerReportingClient.Clients.Fakturoid;
using TimetrackerReportingClient.Clients.TimetrackerReportingClient;
using TimetrackerReportingClient.Email;
using TimetrackerReportingClient.Extensions;
using TimetrackerReportingClient.Helpers;
using TimetrackerReportingClient.Middleware;
using TimetrackerReportingClient.Models.Api;
using TimetrackerReportingClient.Models.CommandLine;
using TimetrackerReportingClient.Models.Fakturoid;

namespace TimetrackerReportingClient;

public class Program
{
    private static void Main(string[] args)
    {
        ExceptionMiddleware.Run(() =>
        {
            CommandLineOptions? cmd = null;
            cmd = cmd!.InitCLI(args);

            var settings = AppSettings.Load();
            var (year, month) = AskForMonth();

            var firstDay = new DateTime(year, month, 1);
            var lastDay = new DateTime(year, month, DateTime.DaysInMonth(year, month));
            Console.WriteLine($"Reporting period: {firstDay:dd.MM.yyyy} – {lastDay:dd.MM.yyyy}");

            // TODO: Registering CLIENT with theyre own ID (nameOf(clientName))
            var client = new TimeTrackerClient(settings.BaseUrl, settings.Token);

            var logs = client.GetWorkLogsForMonth(year, month, cmd.AllUsers);

            Ensure.NotEmpty(logs, "No work logs found for the selected period.");
            //if (logs.Count == 0)
            //{
            //    Console.WriteLine("No work logs found for the selected period.");
            //    return;
            //}

            PrintSummary(logs, year, month);

            if (!string.IsNullOrEmpty(cmd.Format))
                Export(cmd.Format, logs, year, month);

            Console.WriteLine();
            Console.Write("Create invoice in Fakturoid? (y/n): ");
            if (Console.ReadLine()?.Trim().ToLower() == "y")
                CreateFakturoidInvoice(logs, year, month, settings);

            Console.ReadLine();
        });
    }

    private static void PrintSummary(List<WorkLog> logs, int year, int month)
    {
        Console.WriteLine();
        Console.WriteLine($"=== Work Log Summary — {new DateTime(year, month, 1):MMMM yyyy} ===");
        Console.WriteLine();

        // Group by date, sum hours per day
        var byDay = logs.GroupBy(l => l.Timestamp.Date)
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                Date = g.Key,
                TotalHours = g.Sum(l => l.Hours),
                Entries = g.Count(),
            })
            .ToList();

        Console.WriteLine($"{"Date", -14} {"Entries", 8} {"Hours", 10}");
        Console.WriteLine(new string('-', 36));

        foreach (var day in byDay)
        {
            Console.WriteLine(
                $"{day.Date:yyyy-MM-dd,-14} {day.Entries, 8} {day.TotalHours, 10:F2}"
            );
        }

        Console.WriteLine(new string('-', 36));

        double totalHours = logs.Sum(l => l.Hours);
        Console.WriteLine($"{"TOTAL", -14} {logs.Count, 8} {totalHours, 10:F2}");
        Console.WriteLine();
    }

    private static void Export(string format, List<WorkLog> logs, int year, int month)
    {
        var dir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        var fileName = $"worklogs_{year}_{month:D2}";

        if (format.Equals("json", StringComparison.OrdinalIgnoreCase))
        {
            var path = Path.Combine(dir, fileName + ".json");
            File.WriteAllText(path, JsonConvert.SerializeObject(logs, Formatting.Indented));
            Console.WriteLine($"Exported to {path}");
        }
        else if (format.Equals("csv", StringComparison.OrdinalIgnoreCase))
        {
            var path = Path.Combine(dir, fileName + ".csv");
            var lines = new List<string> { "Date,WorkItemId,Hours,Comment,Billable" };
            lines.AddRange(
                logs.Select(l =>
                    $"{l.Timestamp:yyyy-MM-dd},{l.WorkItemId},{l.Hours:F4},{EscapeCsv(l.Comment)},{l.Billable}"
                )
            );
            File.WriteAllLines(path, lines);
            Console.WriteLine($"Exported to {path}");
        }
        else
        {
            Console.WriteLine($"Unknown export format '{format}'. Use 'json' or 'csv'.");
        }
    }

    private static void CreateFakturoidInvoice(
        List<WorkLog> logs,
        int year,
        int month,
        AppSettings settings
    )
    {
        if (
            string.IsNullOrEmpty(settings.FakturoidSlug)
            || string.IsNullOrEmpty(settings.FakturoidClientId)
            || string.IsNullOrEmpty(settings.FakturoidClientSecret)
        )
        {
            Console.WriteLine("Error: Fakturoid credentials missing in appsettings.json.");
            return;
        }
        if (settings.FakturoidSubjectId == 0)
        {
            Console.WriteLine("Error: fakturoidSubjectId is not set in appsettings.json.");
            return;
        }

        // Issue date = last day of the reported month
        var issuedOn = new DateTime(year, month, DateTime.DaysInMonth(year, month));
        var dueOn = issuedOn.AddDays(settings.DueDays);

        // Group work logs by work item, sum hours per item
        var lines = logs.GroupBy(l => l.WorkItemId)
            .Select(g => new InvoiceLine
            {
                Name = $"#{g.Key}",
                Quantity = Math.Round(g.Sum(l => l.Hours), 2)
                    .ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
                Unit = "hod",
                UnitPrice = settings.HourlyRate.ToString(
                    "F2",
                    System.Globalization.CultureInfo.InvariantCulture
                ),
                VatRate = 0,
            })
            .OrderByDescending(l =>
                double.Parse(l.Quantity, System.Globalization.CultureInfo.InvariantCulture)
            )
            .ToList();

        var invoice = new InvoiceRequest
        {
            SubjectId = settings.FakturoidSubjectId,
            IssuedOn = issuedOn.ToString("yyyy-MM-dd"),
            DueOn = dueOn.ToString("yyyy-MM-dd"),
            PaymentMethod = "bank",
            Lines = lines,
        };

        Console.WriteLine($"\nCreating invoice in Fakturoid...");
        Console.WriteLine($"  Issue date : {issuedOn:dd.MM.yyyy}");
        Console.WriteLine($"  Due date   : {dueOn:dd.MM.yyyy}");
        Console.WriteLine($"  Lines      : {lines.Count}");
        Console.WriteLine(
            $"  Total      : {lines.Sum(l => double.Parse(l.Quantity, System.Globalization.CultureInfo.InvariantCulture) * (double)settings.HourlyRate):N2} Kč"
        );

        var fakturoid = new FakturoidClient(
            settings.FakturoidSlug,
            settings.FakturoidClientId,
            settings.FakturoidClientSecret
        );

        var (invoiceId, invoiceNumber, location) = fakturoid.CreateInvoice(invoice);
        Console.WriteLine($"  Invoice created: {location}");

        // ── Download PDF ──────────────────────────────────────────────
        Console.WriteLine("  Downloading invoice PDF…");
        var pdfBytes = fakturoid.DownloadInvoicePdf(invoiceId);
        Console.WriteLine($"  PDF size: {pdfBytes.Length / 1024.0:F1} KB");

        var pdfDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        pdfDir = Path.Combine(pdfDir, "Downloads");
        var pdfPath = Path.Combine(pdfDir, $"invoice_{invoiceNumber}.pdf");
        File.WriteAllBytes(pdfPath, pdfBytes);
        Console.WriteLine($"  Saved to  {pdfPath}");

        // ── Send email ────────────────────────────────────────────────
        if (string.IsNullOrEmpty(settings.EmailFrom) || string.IsNullOrEmpty(settings.EmailTo))
        {
            Console.WriteLine("  Skipping email: emailFrom / emailTo not set in appsettings.json.");
            return;
        }

        Console.Write($"  Send invoice via email to {settings.EmailTo}? (y/n): ");
        if (Console.ReadLine()?.Trim().ToLower() != "y")
            return;

        Console.WriteLine($"  Sending email via provider '{settings.EmailProvider ?? "smtp"}'…");
        var emailSender = EmailSenderFactory.Create(settings);
        emailSender.SendInvoice(invoiceNumber, settings.EmailFrom, settings.EmailTo, pdfBytes);
        Console.WriteLine($"  Email sent to {settings.EmailTo}.");
    }

    private static (int Year, int Month) AskForMonth()
    {
        while (true)
        {
            Console.Write(
                "Which month do you need? (supported formats e.g. April, april, 4, or 04): "
            );
            var input = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(input))
                continue;

            // Try parsing as a month name (English)
            var parsedMonth = input.ParseToMonth();

            return parsedMonth;
        }
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
