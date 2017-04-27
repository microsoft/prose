using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CsvHelper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.ProgramSynthesis.Utils;
using ProseDemo.Web.Models;
using TTExample = Microsoft.ProgramSynthesis.Transformation.Text.Example;
using TTSession = Microsoft.ProgramSynthesis.Transformation.Text.Session;

namespace ProseDemo.Web.Controllers {
    public class HomeController : Controller {
        private const string DataKey = "Data_";
        private readonly IMemoryCache _cache;
        public HomeController(IMemoryCache cache) => _cache = cache;

        public IActionResult Index() => View();

        public IActionResult UploadData([FromForm] int dataLimit) {
            var files = HttpContext.Request.Form.Files;
            if (files.Count != 1) {
                return BadRequest($"Expected a single-file CSV upload, but got {files.Count} files");
            }
            using (Stream stream = files[0].OpenReadStream()) {
                List<string[]> data = LoadData(stream, dataLimit);
                var dataId = Guid.NewGuid();
                _cache.Set(DataKey + dataId, data, new MemoryCacheEntryOptions()
                               .SetPriority(CacheItemPriority.NeverRemove)
                               .SetSlidingExpiration(TimeSpan.FromHours(1)));
                HttpContext.Session.SetString(DataKey, dataId.ToString());
                return Ok();
            }
        }

        [HttpPost]
        public IActionResult TextTransformation([FromBody] TextTransformationRequest request) {
            if (!_cache.TryGetValue(DataKey + HttpContext.Session.GetString(DataKey), out List<string[]> data))
                return BadRequest("No data in the session. Please upload your dataset to the server first.");
            var output = TextTransformation(data, request);
            if (output == null)
                return BadRequest("No program learned");
            return Json(output);
        }

        #region Learning

        public List<string[]> LoadData(Stream stream, int limit = int.MaxValue) {
            var data = new List<string[]>();
            using (var reader = new StreamReader(stream))
            using (var csv = new CsvReader(reader)) {
                while (csv.Read()) {
                    data.Add(csv.CurrentRecord);
                }
            }
            return data.DeterministicallySample(limit).ToList();
        }

        public LearnResponse TextTransformation(List<string[]> data, TextTransformationRequest request) {
            /* TODO */
            throw new NotImplementedException();
        }

        #endregion
    }
}
