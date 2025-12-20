using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

public static class PdfService
{
    public static byte[] GenerateInvoice(EInvoice invoice, Ticket ticket)
    {
        if (invoice == null || ticket == null)
            throw new ArgumentNullException("Invoice or Ticket is null");

        var seatNumbers = ticket.TicketSeats?
            .Where(ts => ts.Seat != null)
            .Select(ts => ts.Seat.SeatNumber)
            .ToList() ?? new List<string>();

        var foodNames = ticket.TicketFoods?
            .Where(tf => tf.FoodItem != null)
            .Select(tf => $"{tf.FoodItem.Name} x{tf.Quantity}")
            .ToList() ?? new List<string>();

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);

                // Default text style
                page.DefaultTextStyle(x => x.FontSize(12));

                page.Content().Column(col =>
                {
                    col.Spacing(10);

                    // ===== Title =====
                    col.Item()
                        .Text("E-Receipt")
                        .FontSize(24)
                        .Bold()
                        .AlignCenter();

                    col.Item().LineHorizontal(1);

                    // ===== Invoice Info =====
                    col.Item().Text($"Invoice Number: {invoice.InvoiceNumber ?? "N/A"}").Bold();
                    col.Item().Text($"Purchase Date: {invoice.PurchaseDate:dd/MM/yyyy HH:mm}");

                    // ===== Ticket Details =====
                    col.Item().PaddingTop(10)
                        .Text("Ticket Details")
                        .FontSize(16)
                        .Bold();

                    col.Item().Text($"Movie: {ticket.Showtime.Movie.Title}");
                    col.Item().Text($"Cinema: {ticket.Showtime.Hall.Cinema.Name}");
                    col.Item().Text($"Hall: {ticket.Showtime.Hall.Name}");
                    col.Item().Text($"Showtime: {ticket.Showtime.StartDateTime:dd MMM yyyy hh:mm tt}");

                    if (seatNumbers.Any())
                        col.Item().Text($"Seats: {string.Join(", ", seatNumbers)}");

                    if (foodNames.Any())
                        col.Item().Text($"Food & Beverages: {string.Join(", ", foodNames)}");
                    else
                        col.Item().Text("Food & Beverages: None");

                    // ===== Payment =====
                    col.Item().PaddingTop(10)
                        .Text("Payment Summary")
                        .FontSize(16)
                        .Bold();

                    col.Item().Text($"Payment Method: {invoice.PaymentMethod ?? "N/A"}");
                    col.Item().Text($"Total Paid: RM {invoice.TotalAmount:F2}")
                        .Bold();
                });
            });
        });

        using var stream = new MemoryStream();
        doc.GeneratePdf(stream);
        return stream.ToArray();
    }
}