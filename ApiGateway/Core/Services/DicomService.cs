using FellowOakDicom;


namespace ApiGateway.Services
{
    public class DicomMetadataResult
    {
        public string Modality { get; set; }
        public string StudyUid { get; set; }
        public string SeriesUid { get; set; }
        public string InstanceUid { get; set; }
        public DateTime? StudyDate { get; set; }
        public string SeriesDescription { get; set; }
        public int? SeriesNumber { get; set; }
        public int? InstanceNumber { get; set; }
        public string PixelSpacing { get; set; }
        public decimal? SliceThickness { get; set; }
        public int? WindowCenter { get; set; }
        public int? WindowWidth { get; set; }
        public int Rows { get; set; }
        public int Columns { get; set; }
        public Dictionary<string, object> AdditionalData { get; set; }
    }

    public class DicomService
    {
        private readonly ILogger<DicomService> _logger;

        public DicomService(ILogger<DicomService> logger)
        {
            _logger = logger;
        }

        public async Task<DicomMetadataResult> ParseDicomFileAsync(string filePath)
        {
            try
            {
                var dicomFile = await DicomFile.OpenAsync(filePath);
                var dataset = dicomFile.Dataset;

                var metadata = new DicomMetadataResult
                {
                    Modality = GetStringValue(dataset, DicomTag.Modality),
                    StudyUid = GetStringValue(dataset, DicomTag.StudyInstanceUID),
                    SeriesUid = GetStringValue(dataset, DicomTag.SeriesInstanceUID),
                    InstanceUid = GetStringValue(dataset, DicomTag.SOPInstanceUID),
                    StudyDate = GetDateValue(dataset, DicomTag.StudyDate),
                    SeriesDescription = GetStringValue(dataset, DicomTag.SeriesDescription),
                    SeriesNumber = GetIntValue(dataset, DicomTag.SeriesNumber),
                    InstanceNumber = GetIntValue(dataset, DicomTag.InstanceNumber),
                    PixelSpacing = GetStringValue(dataset, DicomTag.PixelSpacing),
                    SliceThickness = GetDecimalValue(dataset, DicomTag.SliceThickness),
                    WindowCenter = GetIntValue(dataset, DicomTag.WindowCenter),
                    WindowWidth = GetIntValue(dataset, DicomTag.WindowWidth),
                    Rows = dataset.GetSingleValueOrDefault(DicomTag.Rows, 0),
                    Columns = dataset.GetSingleValueOrDefault(DicomTag.Columns, 0),
                    
                    AdditionalData = new Dictionary<string, object>
                    {
                        { "PatientName", GetStringValue(dataset, DicomTag.PatientName) },
                        { "PatientID", GetStringValue(dataset, DicomTag.PatientID) },
                        { "InstitutionName", GetStringValue(dataset, DicomTag.InstitutionName) },
                        { "Manufacturer", GetStringValue(dataset, DicomTag.Manufacturer) },
                        { "ManufacturerModelName", GetStringValue(dataset, DicomTag.ManufacturerModelName) },
                        { "BitsAllocated", dataset.GetSingleValueOrDefault(DicomTag.BitsAllocated, 0) },
                        { "BitsStored", dataset.GetSingleValueOrDefault(DicomTag.BitsStored, 0) },
                        { "PhotometricInterpretation", GetStringValue(dataset, DicomTag.PhotometricInterpretation) }
                    }
                };

                _logger.LogInformation($"Successfully parsed DICOM file: {filePath}");
                return metadata;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error parsing DICOM file {filePath}: {ex.Message}");
                throw;
            }
        }

        private string GetStringValue(DicomDataset dataset, DicomTag tag)
        {
            try
            {
                return dataset.GetSingleValueOrDefault(tag, string.Empty);
            }
            catch
            {
                return string.Empty;
            }
        }

        private int? GetIntValue(DicomDataset dataset, DicomTag tag)
        {
            try
            {
                var value = dataset.GetSingleValueOrDefault(tag, 0);
                return value == 0 ? null : value;
            }
            catch
            {
                return null;
            }
        }

        private decimal? GetDecimalValue(DicomDataset dataset, DicomTag tag)
        {
            try
            {
                var value = dataset.GetSingleValueOrDefault<decimal>(tag, 0);
                return value == 0 ? null : value;
            }
            catch
            {
                return null;
            }
        }

        private DateTime? GetDateValue(DicomDataset dataset, DicomTag tag)
        {
            try
            {
                var dateString = dataset.GetSingleValueOrDefault(tag, string.Empty);
                if (string.IsNullOrEmpty(dateString))
                    return null;
            
                if (DateTime.TryParseExact(dateString, "yyyyMMdd", 
                        System.Globalization.CultureInfo.InvariantCulture, 
                        System.Globalization.DateTimeStyles.None, 
                        out DateTime result))
                {
                    return result;
                }
        
                return null;
            }
            catch
            {
                return null;
            }
        }
        }
    }
