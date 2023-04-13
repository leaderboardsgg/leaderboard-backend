using FluentValidation;
using Microsoft.Extensions.Options;

namespace LeaderboardBackend;

// from https://andrewlock.net/adding-validation-to-strongly-typed-configuration-objects-using-flentvalidation/

public static class FluentValidationExtensions
{
	public static OptionsBuilder<TOptions> ValidateFluentValidation<TOptions>(this OptionsBuilder<TOptions> optionsBuilder) where TOptions : class
	{
		optionsBuilder.Services.AddSingleton<IValidateOptions<TOptions>>(
			provider => new FluentValidationOptions<TOptions>(optionsBuilder.Name, provider));
		return optionsBuilder;
	}
}

public class FluentValidationOptions<TOptions> : IValidateOptions<TOptions> where TOptions : class
{
	private readonly IServiceProvider _serviceProvider;
	private readonly string? _name;

	public FluentValidationOptions(string? name, IServiceProvider serviceProvider)
	{
		_serviceProvider = serviceProvider;
		_name = name;
	}

	public ValidateOptionsResult Validate(string? name, TOptions options)
	{
		// Null name is used to configure all named options.
		if (_name != null && _name != name)
		{
			// Ignored if not validating this instance.
			return ValidateOptionsResult.Skip;
		}

		// Ensure options are provided to validate against
		ArgumentNullException.ThrowIfNull(options);

		// Validators are registered as scoped, so need to create a scope,
		// as we will be called from the root scope
		using IServiceScope scope = _serviceProvider.CreateScope();
		IValidator<TOptions> validator = scope.ServiceProvider.GetRequiredService<IValidator<TOptions>>();
		FluentValidation.Results.ValidationResult results = validator.Validate(options);
		if (results.IsValid)
		{
			return ValidateOptionsResult.Success;
		}

		string typeName = options.GetType().Name;
		List<string> errors = new();
		foreach (FluentValidation.Results.ValidationFailure? result in results.Errors)
		{
			errors.Add($"Fluent validation failed for '{typeName}.{result.PropertyName}' with the error: '{result.ErrorMessage}'.");
		}

		return ValidateOptionsResult.Fail(errors);
	}
}
