using System;
using System.Linq.Expressions;

namespace TabBlazor.Tests
{
    public class MarkdownSyntaxTests
    {
        private sealed class Address
        {
            public string? Street { get; set; }
        }

        private sealed class Customer
        {
            public string? Name { get; set; }
            public Address Address { get; set; } = new();
            public string?[] Tags { get; set; } = [null];
        }

        [Theory]
        [InlineData(null, "a.exe", "application/x-msdownload", true)]
        [InlineData("*", "a.exe", "", true)]
        [InlineData("image/*", "a.png", "image/png", true)]
        [InlineData("image/*", "a.pdf", "application/pdf", false)]
        [InlineData("image/*,.pdf", "Report.PDF", "", true)]
        [InlineData("application/pdf", "a.pdf", "application/pdf", true)]
        [InlineData(".txt", "a.txt.exe", "", false)]
        public void Accepts_matches_html_accept_syntax(string? accept, string name, string contentType, bool expected)
        {
            Assert.Equal(expected, MarkdownSyntax.Accepts(accept, name, contentType));
        }

        [Theory]
        [InlineData("image/*", true)]
        [InlineData("image/png,.jpg", true)]
        [InlineData("image/*,.pdf", false)]
        [InlineData("*", false)]
        [InlineData(null, false)]
        public void AcceptsOnlyImages_detects_image_only_lists(string? accept, bool expected)
        {
            Assert.Equal(expected, MarkdownSyntax.AcceptsOnlyImages(accept));
        }

        [Fact]
        public void ForUpload_embeds_displayable_images_without_extension_in_text()
        {
            var markdown = MarkdownSyntax.ForUpload(new EditorUploadResult("/a b(1).png"), "my_photo.png", "image/png");

            Assert.Equal(@"![my\_photo](/a%20b%281%29.png)", markdown);
        }

        [Fact]
        public void ForUpload_links_other_types_with_file_name()
        {
            Assert.Equal("[report.tiff](/r)", MarkdownSyntax.ForUpload(new EditorUploadResult("/r"), "report.tiff", "image/tiff"));
        }

        [Fact]
        public void EscapeText_escapes_markdown_and_flattens_line_breaks()
        {
            Assert.Equal(@"a\[b\] \*c\* d", MarkdownSyntax.EscapeText("a[b] *c*\nd"));
        }

        [Fact]
        public void FieldPath_returns_member_chain_from_component_or_closure()
        {
            var customer = new Customer();

            Assert.Equal("customer.Name", MarkdownSyntax.FieldPath((Expression<Func<string?>>)(() => customer.Name)));
            Assert.Equal("customer.Address.Street", MarkdownSyntax.FieldPath((Expression<Func<string?>>)(() => customer.Address.Street)));
        }

        [Fact]
        public void FieldPath_returns_null_for_indexers()
        {
            var customer = new Customer();

            Assert.Null(MarkdownSyntax.FieldPath((Expression<Func<string?>>)(() => customer.Tags[0])));
        }

        [Theory]
        [InlineData(512, "512 B")]
        [InlineData(1536, "1.5 KB")]
        [InlineData(10 * 1024 * 1024, "10 MB")]
        public void FormatSize_uses_binary_units(long bytes, string expected)
        {
            var culture = System.Globalization.CultureInfo.CurrentCulture;
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            try
            {
                Assert.Equal(expected, MarkdownSyntax.FormatSize(bytes));
            }
            finally
            {
                System.Globalization.CultureInfo.CurrentCulture = culture;
            }
        }
    }
}
