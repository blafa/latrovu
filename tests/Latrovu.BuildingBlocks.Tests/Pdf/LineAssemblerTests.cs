using Latrovu.BuildingBlocks.Infrastructure.Pdf;

namespace Latrovu.BuildingBlocks.Tests.Pdf;

public sealed class LineAssemblerTests
{
    private const double Height = 10;

    [Fact]
    public void Assemble_CellsWrittenOutOfOrder_EndUpInTheirRow()
    {
        PositionedWord[] words =
        [
            new("2.5-ZWG", 50, 700, Height), new("1.", 120, 700, Height), new("OG", 135, 700, Height), new("links", 180, 700, Height),
            new("2'370.00", 420, 700, Height),
            new("2.5-ZWG", 50, 680, Height), new("1.", 120, 680, Height), new("OG", 135, 680, Height), new("mitte", 180, 680, Height),
            new("2'020.00", 420, 680, Height),
            new("Vermietet", 500, 700.4, Height),
            new("Vermietet", 500, 679.6, Height)
        ];

        string text = LineAssembler.Assemble(words);

        Assert.Equal("2.5-ZWG 1. OG links 2'370.00 Vermietet\n2.5-ZWG 1. OG mitte 2'020.00 Vermietet", text);
    }

    [Fact]
    public void Assemble_SortsLinesTopToBottomAndWordsLeftToRight()
    {
        PositionedWord[] words = [new("Zürich", 120, 100, Height), new("8045", 50, 100, Height), new("ZYPRESSENSTRASSE", 50, 200, Height), new(" ", 10, 150, Height)];

        Assert.Equal("ZYPRESSENSTRASSE\n8045 Zürich", LineAssembler.Assemble(words));
    }
}
