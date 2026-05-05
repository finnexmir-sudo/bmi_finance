using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FinNex.UI.Configurations
{
    // Sistem az-Latn-AZ kültürü ilə işləyir — "." minlik, "," onluq ayırıcıdır.
    // İstifadəçi formada "3268.92" yazırsa, default binder onu 326892 kimi parse edir.
    // Bu binder həm nöqtəni, həm vergülü onluq ayırıcı kimi qəbul edir.
    public class FlexibleDecimalModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
            if (valueProviderResult == ValueProviderResult.None)
                return Task.CompletedTask;

            bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);

            var raw = valueProviderResult.FirstValue;
            if (string.IsNullOrWhiteSpace(raw))
            {
                if (bindingContext.ModelType == typeof(decimal?))
                    bindingContext.Result = ModelBindingResult.Success(null);
                else
                    bindingContext.Result = ModelBindingResult.Success(0m);
                return Task.CompletedTask;
            }

            // Boşluqları sil
            var s = raw.Trim();

            // İki ayırıcı varsa: sonuncu görünən onluq ayırıcıdır, qalanlarını sil.
            // Məs: "1,234.56" və ya "1.234,56" → "1234.56"
            int lastDot = s.LastIndexOf('.');
            int lastComma = s.LastIndexOf(',');
            int lastSep = Math.Max(lastDot, lastComma);

            if (lastSep >= 0)
            {
                var integerPart = s.Substring(0, lastSep).Replace(",", "").Replace(".", "");
                var decimalPart = s.Substring(lastSep + 1);
                s = integerPart + "." + decimalPart;
            }

            if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var result))
            {
                bindingContext.Result = ModelBindingResult.Success(result);
            }
            else
            {
                bindingContext.ModelState.TryAddModelError(
                    bindingContext.ModelName,
                    $"\"{raw}\" düzgün rəqəm deyil.");
            }

            return Task.CompletedTask;
        }
    }

    public class FlexibleDecimalModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder? GetBinder(ModelBinderProviderContext context)
        {
            if (context.Metadata.ModelType == typeof(decimal) ||
                context.Metadata.ModelType == typeof(decimal?))
            {
                return new FlexibleDecimalModelBinder();
            }
            return null;
        }
    }
}
