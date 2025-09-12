using CX.Container.Server.Databases;
using CX.Engine.DocExtractors.Images;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CX.Container.Server.Controllers.v1;

[ApiController]
[Route("api/page-images")]
[ApiVersion("1.0")]
public sealed class PageImagesController: ControllerBase
{
    private readonly PDFToJpg _pdfToJpg;
    [NotNull] private readonly AelaDbContext _dbContext;

    public PageImagesController([NotNull] PDFToJpg pdfToJpg, [NotNull] AelaDbContext dbContext)
    {
        _pdfToJpg = pdfToJpg ?? throw new ArgumentNullException(nameof(pdfToJpg));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <summary>
    /// Gets a single page image by Document Id and Page No.
    /// </summary>
    [HttpGet("{documentId:guid}/{pageNo:int}", Name = "GetPageImage")]
    public async Task<ActionResult> GetCitation(Guid documentId, int pageNo)
    {
        var document = await _dbContext.SourceDocuments.Where(d => d.Id == documentId).FirstOrDefaultAsync();
        var citation = await _dbContext.Citations.Where(d => d.Id == documentId).FirstOrDefaultAsync();
        
        if (document == null && citation == null)
            return NotFound(new { DocumentId = documentId });
        
        var content = await _pdfToJpg.ImageStore.GetStreamAsync(PDFToJpg.GetPageId(documentId.ToString(), pageNo));
        
        if (content == null)
            return NotFound(new { DocumentId = documentId, PageNo = pageNo });

        return File(content, "image/jpeg", $"{document?.DisplayName ?? citation.Name} #{pageNo}.jpg");
    }
}