using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace LeaderboardBackend;

public class UrlSafeBase64GuidBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext, nameof(bindingContext));

        string modelName = bindingContext.ModelName;
        ValueProviderResult valueProviderResult = bindingContext.ValueProvider.GetValue(modelName);

        if (valueProviderResult == ValueProviderResult.None)
        {
            return Task.CompletedTask;
        }

        string? value = valueProviderResult.FirstValue;

        if (string.IsNullOrEmpty(value))
        {
            return Task.CompletedTask;
        }

        try
        {
            Guid guid = GuidExtensions.FromUrlSafeBase64String(value);
            bindingContext.Result = ModelBindingResult.Success(guid);
        }
        catch (Exception e) when (e is ArgumentException || e is ArgumentNullException || e is FormatException)
        {
            // Failure is already the default so no need to report it here.
        }

        return Task.CompletedTask;
    }
}

public class UrlSafeBase64GuidBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        if (context.Metadata.ModelType == typeof(Guid))
        {
            return new UrlSafeBase64GuidBinder();
        }

        return null;
    }
}
