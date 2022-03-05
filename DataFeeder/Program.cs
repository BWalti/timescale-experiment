// See https://aka.ms/new-console-template for more information

using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using DataFeeder;
using Microsoft.EntityFrameworkCore;
using Spectre.Console;

AnsiConsole.WriteLine("Starting import process...");

await AnsiConsole.Progress()
    .HideCompleted(false)
    .AutoClear(false)
    .Columns(new TaskDescriptionColumn(), new ProgressBarColumn(), new ElapsedTimeColumn(), new PercentageColumn(), new RemainingTimeColumn())
    .StartAsync(async ctx =>
    {
        var csvTask = ctx.AddTask("[green]Reading csv file[/]");
        var dbTask = ctx.AddTask("[green]Importing into DB[/]", autoStart: false);
        var queryTask = ctx.AddTask("[green]Query switzerland 2010-2020[/]", autoStart: false);

        using var reader = new StreamReader("GlobalLandTemperatures_GlobalLandTemperaturesByCountry.csv");
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ","
        });

        var map = csv.Context.AutoMap<CountryTempAverage>();
        map.Map(a => a.Date)
            .Convert(args =>
            {
                var value = args.Row[nameof(CountryTempAverage.Date)];
                var dateOnly = DateOnly.ParseExact(value, "yyyy-MM-dd");
                return dateOnly;
            });
        map.Map(a => a.AverageTemperatureUncertainty)
            .Convert(args =>
            {
                var value = args.Row[nameof(CountryTempAverage.AverageTemperatureUncertainty)];
                if (string.IsNullOrEmpty(value))
                    return null;

                if (double.TryParse(value, out double result))
                {
                    return result;
                }

                return null;
            });
        map.Map(a => a.AverageTemperature)
            .Convert(args =>
            {
                var value = args.Row[nameof(CountryTempAverage.AverageTemperature)];
                if (string.IsNullOrEmpty(value))
                    return null;

                if (double.TryParse(value, out double result))
                {
                    return result;
                }

                return null;
            });

        var averages = csv.GetRecords<CountryTempAverage>()
            .Select(g => g with { Date = g.Date.AddYears(-500) }) // .AddYears(-500)
            .ToList();

        csvTask.StopTask();

        dbTask.MaxValue = averages.Count;
        dbTask.StartTask();

        var batchSize = 300;
        var grouped = averages.GroupBy(a => a.Country).ToList();
        await Parallel.ForEachAsync(
            grouped.Select(g => g.ToList()),
            new ParallelOptions { MaxDegreeOfParallelism = 5 },
            async (g, cancellationToken) =>
            {
                await using var db = new TestDbContext();

                var itemCount = g.Count;
                for (var i = 0; i <= itemCount / batchSize; i++)
                {
                    var count = Math.Min(itemCount - i * batchSize, batchSize);

                    var items = g.Skip(i * batchSize).Take(count);

                    // not sure anymore, if this greatly improves performance
                    await db.BulkInsertAsync(items, cancellationToken);
                    dbTask.Increment(count);
                }
            });

        dbTask.StopTask();

        queryTask.StartTask();
        {
            await using var db = new TestDbContext();

            var switzerlandAverage = await db.Averages
                .Where(x =>
                    x.Date >= new DateOnly(2010, 1, 1)
                    && x.Date < new DateOnly(2020, 1, 1))
                .OrderByDescending(x => x.Date)
                .ToListAsync();

            AnsiConsole.WriteLine($"Found {switzerlandAverage.Count}# entries, ranging from {switzerlandAverage.Min(x => x.AverageTemperature)} to {switzerlandAverage.Max(x => x.AverageTemperature)}");
        }

        queryTask.StopTask();

        AnsiConsole.MarkupLine("[green]Finished[/]!");
    });
