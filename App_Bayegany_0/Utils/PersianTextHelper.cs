using System.Text;

namespace BayeganyApp.Utils
{
    public static class PersianTextHelper
    {
        public static string ToPersianDigits(string? text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text
                .Replace('0', '۰')
                .Replace('1', '۱')
                .Replace('2', '۲')
                .Replace('3', '۳')
                .Replace('4', '۴')
                .Replace('5', '۵')
                .Replace('6', '۶')
                .Replace('7', '۷')
                .Replace('8', '۸')
                .Replace('9', '۹')
                .Replace('ي', 'ی')
                .Replace('ك', 'ک');
        }

        public static string ToEnglishDigits(string? text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text
                .Replace('۰', '0').Replace('٠', '0')
                .Replace('۱', '1').Replace('١', '1')
                .Replace('۲', '2').Replace('٢', '2')
                .Replace('۳', '3').Replace('٣', '3')
                .Replace('۴', '4').Replace('٤', '4')
                .Replace('۵', '5').Replace('٥', '5')
                .Replace('۶', '6').Replace('٦', '6')
                .Replace('۷', '7').Replace('٧', '7')
                .Replace('۸', '8').Replace('٨', '8')
                .Replace('۹', '9').Replace('٩', '9')
                .Replace('ي', 'ی')
                .Replace('ك', 'ک');
        }

        public static string Normalize(string? text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            var sb = new StringBuilder(text.Length);
            foreach (char ch in text)
            {
                switch (ch)
                {
                    case '۰': case '٠': sb.Append('0'); break;
                    case '۱': case '١': sb.Append('1'); break;
                    case '۲': case '٢': sb.Append('2'); break;
                    case '۳': case '٣': sb.Append('3'); break;
                    case '۴': case '٤': sb.Append('4'); break;
                    case '۵': case '٥': sb.Append('5'); break;
                    case '۶': case '٦': sb.Append('6'); break;
                    case '۷': case '٧': sb.Append('7'); break;
                    case '۸': case '٨': sb.Append('8'); break;
                    case '۹': case '٩': sb.Append('9'); break;

                    case 'ي': case 'ى': case 'ئ': sb.Append('ی'); break;
                    case 'ك': sb.Append('ک'); break;
                    case 'ة': sb.Append('ه'); break;
                    case '\u200c': sb.Append(' '); break;

                    default:
                        sb.Append(char.ToLowerInvariant(ch));
                        break;
                }
            }
            return sb.ToString().Trim();
        }
    }
}
