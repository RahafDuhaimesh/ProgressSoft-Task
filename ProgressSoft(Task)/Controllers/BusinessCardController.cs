using CsvHelper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProgressSoft_Task_.DTO_s;
using ProgressSoft_Task_.Models;
using ProgressSoft_Task_.Services;
using System.Globalization;
using OfficeOpenXml;
using System.IO;
using ZXing;
using System.Xml.Serialization; 
using ZXing.Common;
using ZXing.Rendering;
using System.Drawing;
using ZXing.QrCode;
using Azure.Core;
using System;
using ZXing.Windows.Compatibility;

namespace ProgressSoft_Task_.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BusinessCardController : ControllerBase
    {
        private readonly MyDbContext _db;
        private readonly ICSVService _csvService;

        public BusinessCardController(MyDbContext db, ICSVService cSVService)
        {
            _db = db;
            _csvService = cSVService;
        }
        ///////////////////////////////////////////////////////////////////////////////////////////////
        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> CreateCard([FromForm] CardRequestDTO cardRequest)
        {
            if (cardRequest == null || string.IsNullOrEmpty(cardRequest.Name))
            {
                return BadRequest("Invalid card data.");
            }

            DateOnly? dateOfBirth = null;
            if (cardRequest.DateOfBirthYear.HasValue && cardRequest.DateOfBirthMonth.HasValue && cardRequest.DateOfBirthDay.HasValue)
            {
                var dob = new DateTime(cardRequest.DateOfBirthYear.Value, cardRequest.DateOfBirthMonth.Value, cardRequest.DateOfBirthDay.Value);
                dateOfBirth = DateOnly.FromDateTime(dob);
            }

            var businessCard = new BusinessCard
            {
                Name = cardRequest.Name,
                Gender = cardRequest.Gender,
                DateOfBirth = dateOfBirth, 
                Email = cardRequest.Email,
                Phone = cardRequest.Phone,
                Photo = !string.IsNullOrEmpty(cardRequest.Photo)
            ? Convert.FromBase64String(cardRequest.Photo)
            : null,
                Address = cardRequest.Address
            };

            _db.BusinessCards.Add(businessCard);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(CreateCard), new { id = businessCard.Id }, businessCard);
        }


        ///////////////////////////////////////////////////////////////////////////////////////////////
        [HttpGet]
        public async Task<IActionResult> GetAllCards()
        {
            var cards = await _db.BusinessCards.ToListAsync();
            return Ok(cards);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        [HttpGet("DownLoadALLCards")]
        public async Task<IActionResult> DownLoadALLCards()
        {
            var cards = await _db.BusinessCards.ToListAsync();
            if (cards == null) { return NotFound(); }

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("BusinessCard");

                worksheet.Cells[1, 1].Value = "ID";
                worksheet.Cells[1, 2].Value = "Name";
                worksheet.Cells[1, 3].Value = "Gender";
                worksheet.Cells[1, 4].Value = "DateOfBirth";
                worksheet.Cells[1, 5].Value = "Email";
                worksheet.Cells[1, 6].Value = "Phone";
                worksheet.Cells[1, 7].Value = "Address";
                for (int i = 0; i < cards.Count; i++)
                {
                    var card = cards[i];
                    worksheet.Cells[i + 2, 1].Value = card.Id;
                    worksheet.Cells[i + 2, 2].Value = card.Name;
                    worksheet.Cells[i + 2, 3].Value = card.Gender;
                    worksheet.Cells[i + 2, 4].Value = card.DateOfBirth?.ToString("yyyy-MM-dd");
                    worksheet.Cells[i + 2, 5].Value = card.Email;
                    worksheet.Cells[i + 2, 6].Value = card.Phone;
                    worksheet.Cells[i + 2, 7].Value = card.Address;
                }

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "BusinessCard.xlsx");
            }
        }
        ///////////////////////////////////////////////////////////////////////////////////////////////
        [HttpGet("GetCardByID")]
        public async Task<IActionResult> GetCardByID(int id)
        {
            if (id <= 0) { return BadRequest(); }

            var card = await _db.BusinessCards.FirstOrDefaultAsync(x => x.Id == id);

            if (card == null) { return NotFound(); }
            return Ok(card);
        }
        ///////////////////////////////////////////////////////////////////////////////////////////////
        [HttpGet("GetCardByIDEXCEL")]
        public async Task<IActionResult> GetCardByIDEXCEL(int id)
        {
            if (id <= 0) { return BadRequest(); }

            var card = await _db.BusinessCards.FirstOrDefaultAsync(x => x.Id == id); 

            if (card == null) { return NotFound(); }

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("BusinessCard");

                worksheet.Cells[1, 1].Value = "ID";
                worksheet.Cells[1, 2].Value = "Name";
                worksheet.Cells[1, 3].Value = "Gender";
                worksheet.Cells[1, 4].Value = "DateOfBirth";
                worksheet.Cells[1, 5].Value = "Email";
                worksheet.Cells[1, 6].Value = "Phone";
                worksheet.Cells[1, 7].Value = "Address";

                worksheet.Cells[2, 1].Value = card.Id;
                worksheet.Cells[2, 2].Value = card.Name;
                worksheet.Cells[2, 3].Value = card.Gender;
                worksheet.Cells[2, 4].Value = card.DateOfBirth?.ToString("yyyy-MM-dd"); 
                worksheet.Cells[2, 5].Value = card.Email;
                worksheet.Cells[2, 6].Value = card.Phone;
                worksheet.Cells[2, 7].Value = card.Address;

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "BusinessCard.xlsx");
            }
        }
        ///////////////////////////////////////////////////////////////////////////////////////////////
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCard(int id)
        {
            var card = await _db.BusinessCards.FindAsync(id);
            if (card == null)
            {
                return NotFound();
            }

            _db.BusinessCards.Remove(card);
            await _db.SaveChangesAsync();

            return NoContent();
        }
        ///////////////////////////////////////////////////////////////////////////////////////////////

        [HttpGet("GenerateQRCodeForCard")]
        public IActionResult GenerateQRCodeForCard(int id)
        {
            if (id <= 0)
            {
                return BadRequest("Invalid ID.");
            }

            string downloadLink = Url.Action("GetCardByID", "BusinessCard", new { id }, Request.Scheme);

            if (string.IsNullOrWhiteSpace(downloadLink))
            {
                return BadRequest("Download link could not be generated.");
            }

            var barcodeWriter = new BarcodeWriter
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new ZXing.Common.EncodingOptions
                {
                    Width = 300,
                    Height = 300
                }
            };

            using (var bitmap = barcodeWriter.Write(downloadLink)) 
            {
                using (var stream = new MemoryStream())
                {
                    bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                    var fileContents = stream.ToArray();
                    return File(fileContents, "image/png", "BusinessCardQRCode.png"); 
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        [HttpPost("GetBusinessCardCSV")]
        public async Task<IActionResult> GetBusinessCardCSV([FromForm] IFormFileCollection file)
        {
            if (file == null || file.Count == 0)
            {
                return BadRequest("No file uploaded.");
            }

            try
            {
                using (var reader = new StreamReader(file[0].OpenReadStream()))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    var cardRequests = csv.GetRecords<CardRequestDTO>().ToList();
                    var businessCards = cardRequests.Select(cardRequest => new BusinessCard
                    {
                        Name = cardRequest.Name,
                        Gender = cardRequest.Gender,
                        DateOfBirth = (cardRequest.DateOfBirthYear.HasValue &&
                                       cardRequest.DateOfBirthMonth.HasValue &&
                                       cardRequest.DateOfBirthDay.HasValue)
                                      ? new DateOnly(cardRequest.DateOfBirthYear.Value,
                                                      cardRequest.DateOfBirthMonth.Value,
                                                      cardRequest.DateOfBirthDay.Value)
                                      : (DateOnly?)null,
                        Email = cardRequest.Email,
                        Phone = cardRequest.Phone,
                        Photo = !string.IsNullOrEmpty(cardRequest.Photo)
                                ? Convert.FromBase64String(cardRequest.Photo)
                                : null,
                        Address = cardRequest.Address
                    }).ToList();

               
                    _db.BusinessCards.AddRange(businessCards);
                    await _db.SaveChangesAsync();

                    return Ok(businessCards); 
                }
            }
            catch (CsvHelperException ex)
            {
                return BadRequest($"CSV Error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return BadRequest($"An unexpected error occurred: {ex.Message}");
            }
        }


        ///////////////////////////////////////////////////////////////////////////////////////////////
        [HttpPost("GetBusinessCardXML")]
        public async Task<IActionResult> GetBusinessCardXML([FromForm] IFormFile xmlFile)
        {
            if (xmlFile == null)
            {
                return BadRequest("No XML file uploaded.");
            }

            try
            {
                using (var reader = new StreamReader(xmlFile.OpenReadStream()))
                {
                    var serializer = new XmlSerializer(typeof(List<CardRequestDTO>), new XmlRootAttribute("BusinessCards"));
                    var cardRequests = (List<CardRequestDTO>)serializer.Deserialize(reader);

                    var businessCards = cardRequests.Select(cardRequest => new BusinessCard
                    {
                        Name = cardRequest.Name,
                        Gender = cardRequest.Gender,
                        DateOfBirth = (cardRequest.DateOfBirthYear.HasValue &&
                                       cardRequest.DateOfBirthMonth.HasValue &&
                                       cardRequest.DateOfBirthDay.HasValue)
                                      ? new DateOnly(cardRequest.DateOfBirthYear.Value,
                                                      cardRequest.DateOfBirthMonth.Value,
                                                      cardRequest.DateOfBirthDay.Value)
                                      : (DateOnly?)null,
                        Email = cardRequest.Email,
                        Phone = cardRequest.Phone,
                        Photo = !string.IsNullOrEmpty(cardRequest.Photo)
                                ? Convert.FromBase64String(cardRequest.Photo)
                                : null,
                        Address = cardRequest.Address
                    }).ToList();

                    // إضافة البيانات إلى قاعدة البيانات
                    _db.BusinessCards.AddRange(businessCards);
                    await _db.SaveChangesAsync();

                    return Ok(businessCards);
                }
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest($"XML Error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return BadRequest($"An unexpected error occurred: {ex.Message}");
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        [HttpGet("filter")]
        public async Task<IActionResult> FilterCards(string? name, string? email, string? phone, string? gender)
        {
            var query = _db.BusinessCards.AsQueryable();

            if (!string.IsNullOrEmpty(name))
                query = query.Where(c => c.Name.Contains(name));
            if (!string.IsNullOrEmpty(email))
                query = query.Where(c => c.Email.Contains(email));
            if (!string.IsNullOrEmpty(phone))
                query = query.Where(c => c.Phone.Contains(phone));
            if (!string.IsNullOrEmpty(gender))
                query = query.Where(c => c.Gender.Contains(gender));

            var filteredCards = await query.ToListAsync();
            return Ok(filteredCards);
        }
    }
}
