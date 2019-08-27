# OCR-WebAPI-AND-CLIENT-SANDEEP-KANAO


Support 3 file types for OCR: Pdf, Tiff and Zip

Loop through all extracted files. If the file is .tiff or .pdf, the corresponding OCR algorithm will be executed to get text from that file.

The TiffController is based on BaseController has only one Post action to receive file from clients.

public class TiffController : BaseController
{
    public Task<IEnumerable<HDFile>> Post()
    {
        return Handle(new List<string>() { ".tif", ".tiff" });
    }
}

When this Post function gets called, I’ll call the Handle function from base class with parameters to define which file types I want to handle. For example, in TiffController I only want to handle “.tif” or “.tiff” files.

protected virtual Task<IEnumerable<HDFile>> Handle(IEnumerable<string> fileExtensions)
{
    try
    {
        var uploadFolderPath = HostingEnvironment.MapPath("~/App_Data/" + UploadFolder);
        log.Debug(uploadFolderPath);
 
        if (Request.Content.IsMimeMultipartContent())
        {
            var streamProvider = new WithExtensionMultipartFormDataStreamProvider(uploadFolderPath);
            var task = Request.Content.ReadAsMultipartAsync(streamProvider).ContinueWith<IEnumerable<HDFile>>(t =>
            {
                if (t.IsFaulted || t.IsCanceled)
                {
                    throw new HttpResponseException(HttpStatusCode.InternalServerError);
                }
 
                return Handle(streamProvider.FileData.Select(x => new HDFile(x.Headers.ContentDisposition.FileName, null, x.LocalFileName)), fileExtensions);
            });
 
            return task;
        }
        else
        {
            throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotAcceptable, "This request is not properly formatted"));
        }
    }
    catch (Exception ex)
    {
        log.Error(ex);
        throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message));
    }
}
 
protected IEnumerable<HDFile> Handle(IEnumerable<HDFile> files, IEnumerable<string> fileExtensions)
{
    files = files.Where(x => fileExtensions.Contains(Path.GetExtension(x.Name), StringComparer.OrdinalIgnoreCase)).ToList();
 
    foreach (var item in files)
    {
        foreach (var engine in OCREngines.GetDefaultInstance().AllRegisteredEngines)
        {
            if (engine.CanHandle(Path.GetExtension(item.Name)))
            {
                item.Text = engine.GetText(item.Tag);
                break;
            }
        }
    }
    return files;
}


In Handle function, I’ll upload file to App_Data/uploads folder and run OCR on it. For each file type I define an OCR engine for it. All of these engines implement IOCREngine with following functions

public interface IOCREngine
{
    bool CanHandle(string fileExtensions);
 
    string GetText(string filePath);
}
 
public class TiffOCREngine : IOCREngine
{
    public bool CanHandle(string fileExtensions)
    {
        return fileExtensions.Equals(".tif", System.StringComparison.OrdinalIgnoreCase) || fileExtensions.Equals(".tiff", System.StringComparison.OrdinalIgnoreCase);
    }
 
    public string GetText(string filePath)
    {
        return TesseractUtil.GetText(filePath);
    }
}

The engine is only a wrapper for 3rd OCR library which I use for testing. In this case, TiffOCREngine just simply call function of Tesseract framework.

3. Zip
The ZipController doesn’t work like Tiff/PdfController because the files can’t be directly handled. I have to override the Handle function to extract the files first and let selective strategy run through all extracted files in temporary folder.

public class ZipController : BaseController
{
    private ILog log = log4net.LogManager.GetLogger(typeof(ZipController));
 
    public Task<IEnumerable<HDFile>> Post()
    {
        return Handle(new List<string>() { ".zip" });
    }
 
    protected override Task<IEnumerable<HDFile>> Handle(IEnumerable<string> fileExtensions)
    {
        try
        {
            var uploadFolderPath = HostingEnvironment.MapPath("~/App_Data/" + UploadFolder);
            log.Debug(uploadFolderPath);
 
            if (Request.Content.IsMimeMultipartContent())
            {
                var streamProvider = new WithExtensionMultipartFormDataStreamProvider(uploadFolderPath);
                var task = Request.Content.ReadAsMultipartAsync(streamProvider).ContinueWith<IEnumerable<HDFile>>(t =>
                {
                    if (t.IsFaulted || t.IsCanceled)
                    {
                        throw new HttpResponseException(HttpStatusCode.InternalServerError);
                    }
 
                    IEnumerable<string> zipFilePaths = streamProvider.FileData.Where(x => fileExtensions.Contains(Path.GetExtension(x.LocalFileName), StringComparer.OrdinalIgnoreCase)).Select(x => x.LocalFileName);
 
                    List<string> files = new List<string>();
                    foreach (var zipFilePath in zipFilePaths)
                    {
                        string tempFolder = Path.Combine(Path.GetTempPath(), CryptoUtil.MD5(DateTime.Now.Ticks.ToString() + zipFilePath));
                        if (!Directory.Exists(tempFolder))
                            Directory.CreateDirectory(tempFolder);
                        ZipFile zipFile = new ZipFile(zipFilePath);
                        zipFile.ExtractAll(tempFolder);
                        files.AddRange(Directory.GetFiles(tempFolder));
                    }
 
                    IList<HDFile> result = new List<HDFile>();
                    foreach (var item in files)
                    {
                        foreach (var engine in OCREngines.GetDefaultInstance().AllRegisteredEngines)
                        {
                            if (engine.CanHandle(Path.GetExtension(item)))
                            {
                                result.Add(new HDFile(Path.GetFileName(item), engine.GetText(item)));
                                break;
                            }
                        }
                    }
                    return result;
                });
 
                return task;
            }
            else
            {
                throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotAcceptable, "This request is not properly formatted"));
            }
        }
        catch (Exception ex)
        {
            log.Error(ex);
            throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message));
        }
    }
}


When the web service is ready, we can easily consume it from the client. Below is an example of .NET console client

The client will post sequentially files to corresponding controllers, get the result and print extracted text to the console. The image below shows the result of Tesseract.
