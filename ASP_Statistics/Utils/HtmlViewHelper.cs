using ASP_Statistics.Enums;

namespace ASP_Statistics.Utils
{
    public static class HtmlViewHelper
    {
        public static string GetCssClassForBorderBy(int? threadNumber)
        {
            switch (threadNumber)
            {
                case 0:
                    return "border-success";
                case 1:
                    return "border-danger";
                case 2:
                    return "border-warning";
                case 3:
                    return "border-primary";
                default:
                    return "border-dark";
            }
        }

        public static string GetCssClassForBackGround(int? threadNumber)
        {
            switch (threadNumber)
            {
                case 0:
                    return "bg-success";
                case 1:
                    return "bg-danger";
                case 2:
                    return "bg-warning";
                case 3:
                    return "bg-primary";
                default:
                    return "bg-dark";
            }
        }

        public static string GetCssClassForTextColor(int? threadNumber)
        {
            switch (threadNumber)
            {
                case 0:
                    return "text-success";
                case 1:
                    return "text-danger";
                case 2:
                    return "text-warning";
                case 3:
                    return "text-primary";
                default:
                    return "text-dark";
            }
        }

        public static string GetCssClassForTable(GameResultType gameResultType)
        {
            switch (gameResultType)
            {
                case GameResultType.Expectation:
                    return "";
                case GameResultType.Win:
                    return "table-success";
                case GameResultType.Defeat:
                    return "table-danger";
                case GameResultType.RefundOrCancellation:
                    return "table-warning";
                default:
                    return "";
            }
        }

        public static string GetColor(GameResultType gameResultType)
        {
            switch (gameResultType)
            {
                case GameResultType.Expectation:
                    return "#6c757d";
                case GameResultType.Win:
                    return "#28a745";
                case GameResultType.Defeat:
                    return "#dc3545";
                case GameResultType.RefundOrCancellation:
                    return "#ffc107";
                default:
                    return "#343a40";
            }
        }
    }
}