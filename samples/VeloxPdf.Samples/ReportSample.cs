namespace VeloxPdf.Samples;
using VeloxPdf.Document;
using VeloxPdf.Templates;

public static class ReportSample
{
    public static byte[] Generate()
    {
        var data = new TemplateData
        {
            Title = "Q3 2026 Business Performance Report",
            Metadata = new PdfMetadata
            {
                Title = "Q3 2026 Business Performance Report",
                Author = "Analytics Team",
                Subject = "Quarterly business performance analysis"
            }
        };

        data.Properties["ReportTitle"] = "Q3 2026 Business Performance Report";
        data.Properties["Author"] = "Analytics Team";
        data.Properties["Date"] = "September 11, 2026";

        var sections = new List<ReportSection>
        {
            new("Executive Summary",
                "This quarterly report provides a comprehensive overview of our business performance " +
                "for Q3 2026. Key highlights include a 23% increase in revenue, successful launch of " +
                "three new product lines, and expansion into two new markets. Overall the company has " +
                "demonstrated strong growth momentum and is well-positioned for continued success."),

            new("Revenue Analysis",
                "Total revenue for Q3 2026 reached $12.4 million, representing a 23% year-over-year " +
                "increase. Product sales contributed $8.2 million (66%), while service revenue added " +
                "$3.1 million (25%). Subscription revenue grew to $1.1 million (9%), showing a 45% " +
                "increase from the same period last year. The EMEA region showed the strongest growth " +
                "at 34%, followed by APAC at 28% and North America at 18%."),

            new("Product Performance",
                "Our flagship product VeloxPDF continued to be the top revenue contributor, " +
                "generating $5.2 million in license fees. The newly launched CloudSync product exceeded " +
                "initial projections by 40%, while the Analytics Suite showed moderate adoption with " +
                "room for improvement. Customer retention rate improved to 94%, up from 91% in Q2."),

            new("Market Expansion",
                "Q3 marked our successful entry into the Australian and Canadian markets. " +
                "Initial market reception has been positive, with 47 enterprise clients onboarded " +
                "within the first 60 days of operations. Local partnership agreements were signed " +
                "with leading regional technology distributors in both markets."),

            new("Operations & Efficiency",
                "Operational efficiency improved significantly through the implementation of automated " +
                "workflows and AI-assisted customer support. Support ticket resolution time decreased " +
                "by 38%, and the Net Promoter Score increased from 42 to 61. Headcount grew by 12 FTEs " +
                "to support expansion initiatives, while maintaining a healthy revenue per employee ratio."),

            new("Financial Outlook",
                "Based on current performance trends and pipeline analysis, Q4 2026 revenue is " +
                "projected to reach $14-15 million. Key growth drivers include the planned enterprise " +
                "tier launch, a major system integrator partnership expected to close, and the full " +
                "quarter contribution from our new market expansions."),

            new("Risk Assessment",
                "Primary risks include increased competitive pressure from two well-funded startups " +
                "entering our core market, potential supply chain disruptions affecting our hardware " +
                "partnerships, and evolving regulatory requirements in the EU market. Mitigation " +
                "strategies are in place for each identified risk."),

            new("Customer Success Metrics",
                "Customer satisfaction scores reached an all-time high of 4.6/5.0. Enterprise client " +
                "count grew to 234, up from 198 at the end of Q2. Total Active Users increased by 31% " +
                "to 89,000. Churn rate decreased to 2.1% monthly, well below industry average of 3.5%."),

            new("Technology Investment",
                "R&D investment totaled $2.1 million in Q3, focused on machine learning capabilities, " +
                "API platform improvements, and mobile application development. Three major feature " +
                "releases were shipped, receiving highly positive user feedback and contributing to " +
                "the improved retention metrics."),

            new("Conclusion & Strategic Priorities",
                "Q3 2026 demonstrates strong execution against our strategic objectives. For Q4 2026, " +
                "our priorities are: (1) Successfully launch the Enterprise tier, (2) Close the " +
                "strategic partnership, (3) Achieve profitability in new markets, and (4) Reduce " +
                "support costs by 20% through automation. We remain confident in our full-year targets.")
        };

        data.Data["Sections"] = sections;

        var template = new ReportTemplate();
        var doc = template.Build(data);
        return doc.Save();
    }
}
