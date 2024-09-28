# ProgressSoft-Task

To Download the card details as excel file:
1- Install-Package EPPlus
2- using OfficeOpenXml;
using System.IO;
3-  using (var package = new ExcelPackage())
    {
        var worksheet = package.Workbook.Worksheets.Add("BusinessCard");
....

        var stream = new MemoryStream();
        package.SaveAs(stream);
        stream.Position = 0;

        return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "BusinessCard.xlsx");
    }
    -------------------------------------------------------------------------------------------------
    -To Make QR Code:
1- Install-Package ZXing.Net
2- using ZXing;
using System.Drawing;
3- Install ZXing.Net.Bindings.Windows.Compatibility
-------------------------------------------------------------------------------------------------
- to get data from xml:
1- using System.Xml.Serialization;
  2- builder.Services.AddControllers()
    .AddXmlSerializerFormatters(); // لإضافة دعم XML
