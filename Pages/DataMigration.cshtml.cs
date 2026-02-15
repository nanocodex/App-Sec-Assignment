using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApplication1.Services;

namespace WebApplication1.Pages
{
    [Authorize]
    public class DataMigrationModel : PageModel
    {
        private readonly IDataMigrationService _migrationService;
        private readonly ILogger<DataMigrationModel> _logger;

        public DataMigrationModel(
            IDataMigrationService migrationService,
            ILogger<DataMigrationModel> logger)
        {
            _migrationService = migrationService;
            _logger = logger;
        }

        public DataMigrationResult? MigrationResult { get; set; }
        public bool HasRun { get; set; }

        public void OnGet()
        {
            HasRun = false;
        }

        public async Task<IActionResult> OnPostEncodeAddressesAsync()
        {
            _logger.LogInformation("User {Email} initiated address encoding migration", User.Identity?.Name);

            try
            {
                MigrationResult = await _migrationService.EncodeExistingAddressFieldsAsync();
                HasRun = true;

                if (MigrationResult.Success)
                {
                    TempData["SuccessMessage"] = $"Migration completed successfully! Updated {MigrationResult.RecordsUpdated} out of {MigrationResult.RecordsProcessed} records.";
                }
                else
                {
                    TempData["ErrorMessage"] = $"Migration completed with errors. Updated {MigrationResult.RecordsUpdated}, Failed {MigrationResult.RecordsFailed}.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during migration");
                TempData["ErrorMessage"] = $"Migration failed: {ex.Message}";
                HasRun = true;
            }

            return Page();
        }
    }
}
