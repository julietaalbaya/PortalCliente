using Microsoft.AspNetCore.Http.HttpResults;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

// Configuración para JSON
var jsonOptions = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = true
};

// Devuelve la ruta completa del archivo compras.json
string ComprasFilePath() => Path.Combine(app.Environment.ContentRootPath, "data", "compras.json");  // Búsqueda en carpeta 'data'

// Lee el archivo compras.json (si existe) y deserializa su contenido
async Task<ComprasData> LoadComprasAsync()
{
    var path = ComprasFilePath();
    if (!File.Exists(path)) return new ComprasData();
    var json = await File.ReadAllTextAsync(path);
    var data = JsonSerializer.Deserialize<ComprasData>(json, jsonOptions);
    return data ?? new ComprasData();
}

// Convierte el objeto ComprasData a JSON y lo guarda en el archivo compras.json
async Task SaveComprasAsync(ComprasData data)
{
    var path = ComprasFilePath();
    var json = JsonSerializer.Serialize(data, jsonOptions);
    var dir = Path.GetDirectoryName(path);
    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
    await File.WriteAllTextAsync(path, json);
}

// --- MISDATOS helpers and endpoints ---
// Devuelve la ruta completa del archivo misdatos.json
string MisDatosFilePath() => Path.Combine(app.Environment.ContentRootPath, "data", "misdatos.json");

// Lee misdatos.json (si existe)
async Task<MisDatos?> LoadMisDatosAsync()
{
    var path = MisDatosFilePath();
    if (!File.Exists(path)) return null;
    var json = await File.ReadAllTextAsync(path);
    var data = JsonSerializer.Deserialize<MisDatos>(json, jsonOptions);
    return data;
}

// Guarda misdatos.json
async Task SaveMisDatosAsync(MisDatos data)
{
    var path = MisDatosFilePath();
    var json = JsonSerializer.Serialize(data, jsonOptions);
    var dir = Path.GetDirectoryName(path);
    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
    await File.WriteAllTextAsync(path, json);
}

// GET misdatos
app.MapGet("/misdatos", async () =>
{
    var data = await LoadMisDatosAsync();
    return data is null ? Results.NotFound() : Results.Json(data, jsonOptions);
});

// POST misdatos (create if not exists)
app.MapPost("/misdatos", async (MisDatos nuevos) =>
{
    var existing = await LoadMisDatosAsync();
    if (existing is not null) return Results.Conflict("misdatos already exists. Use PUT to update.");
    await SaveMisDatosAsync(nuevos);
    return Results.Created("/misdatos", nuevos);
});

// PUT misdatos (create or update)
app.MapPut("/misdatos", async (MisDatos updated) =>
{
    await SaveMisDatosAsync(updated);
    return Results.NoContent();
});

// DELETE misdatos (remove file)
app.MapDelete("/misdatos", async () =>
{
    var path = MisDatosFilePath();
    if (!File.Exists(path)) return Results.NotFound();
    File.Delete(path);
    return Results.NoContent();
});

// GET all (optionally filtered by ?estado=...)
app.MapGet("/compras", async (string? estado) =>
{
    var data = await LoadComprasAsync();
    var list = string.IsNullOrWhiteSpace(estado)
        ? data.Compras
        : data.Compras.Where(c => string.Equals(c.Estado, estado, StringComparison.OrdinalIgnoreCase)).ToList();
    return Results.Json(new ComprasData { Compras = list }, jsonOptions);
});

// GET by id
app.MapGet("/compras/{id}", async (string id) =>
{
    var data = await LoadComprasAsync();
    var item = data.Compras.FirstOrDefault(c => string.Equals(c.Id, id, StringComparison.OrdinalIgnoreCase));
    return item is null ? Results.NotFound() : Results.Json(item, jsonOptions);
});

// POST - create (for testing)
app.MapPost("/compras", async (Compra compra) =>
{
    var data = await LoadComprasAsync();
    if (data.Compras.Any(c => string.Equals(c.Id, compra.Id, StringComparison.OrdinalIgnoreCase)))
        return Results.Conflict($"Compra with id '{compra.Id}' already exists.");
    data.Compras.Add(compra);
    await SaveComprasAsync(data);
    return Results.Created($"/compras/{compra.Id}", compra);
});

// PUT - update (for testing)
app.MapPut("/compras/{id}", async (string id, Compra updated) =>
{
    var data = await LoadComprasAsync();
    var idx = data.Compras.FindIndex(c => string.Equals(c.Id, id, StringComparison.OrdinalIgnoreCase));
    if (idx == -1) return Results.NotFound();
    updated.Id = id; // ensure id consistency
    data.Compras[idx] = updated;
    await SaveComprasAsync(data);
    return Results.NoContent();
});

// DELETE (for testing)
app.MapDelete("/compras/{id}", async (string id) =>
{
    var data = await LoadComprasAsync();
    var removed = data.Compras.RemoveAll(c => string.Equals(c.Id, id, StringComparison.OrdinalIgnoreCase));
    if (removed == 0) return Results.NotFound();
    await SaveComprasAsync(data);
    return Results.NoContent();
});

// --- MOVIMIENTOS helpers and endpoints ---
// Devuelve la ruta completa del archivo movimientos.json
string MovimientosFilePath() => Path.Combine(app.Environment.ContentRootPath, "data", "movimientos.json");

// Lee movimientos.json (si existe)
async Task<MovimientosData> LoadMovimientosAsync()
{
    var path = MovimientosFilePath();
    if (!File.Exists(path)) return new MovimientosData();
    var json = await File.ReadAllTextAsync(path);
    var data = JsonSerializer.Deserialize<MovimientosData>(json, jsonOptions);
    return data ?? new MovimientosData();
}

// Guarda movimientos.json
async Task SaveMovimientosAsync(MovimientosData data)
{
    var path = MovimientosFilePath();
    var json = JsonSerializer.Serialize(data, jsonOptions);
    var dir = Path.GetDirectoryName(path);
    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
    await File.WriteAllTextAsync(path, json);
}

// GET all movimientos
app.MapGet("/movimientos", async () =>
{
    var data = await LoadMovimientosAsync();
    return Results.Json(data, jsonOptions);
});

// GET movimiento by index (0-based)
app.MapGet("/movimientos/{index:int}", async (int index) =>
{
    var data = await LoadMovimientosAsync();
    if (index < 0 || index >= data.Movimientos.Count) return Results.NotFound();
    return Results.Json(data.Movimientos[index], jsonOptions);
});

// POST - add movimiento
app.MapPost("/movimientos", async (Movimiento mov) =>
{
    var data = await LoadMovimientosAsync();
    data.Movimientos.Add(mov);
    await SaveMovimientosAsync(data);
    var idx = data.Movimientos.Count - 1;
    return Results.Created($"/movimientos/{idx}", mov);
});

// PUT - update movimiento by index
app.MapPut("/movimientos/{index:int}", async (int index, Movimiento updated) =>
{
    var data = await LoadMovimientosAsync();
    if (index < 0 || index >= data.Movimientos.Count) return Results.NotFound();
    data.Movimientos[index] = updated;
    await SaveMovimientosAsync(data);
    return Results.NoContent();
});

// DELETE movimiento by index
app.MapDelete("/movimientos/{index:int}", async (int index) =>
{
    var data = await LoadMovimientosAsync();
    if (index < 0 || index >= data.Movimientos.Count) return Results.NotFound();
    data.Movimientos.RemoveAt(index);
    await SaveMovimientosAsync(data);
    return Results.NoContent();
});

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

public class Compra
{
    public string Id { get; set; } = string.Empty;
    public decimal Precio { get; set; }
    public string Estado { get; set; } = string.Empty;
}

public class ComprasData
{
    public List<Compra> Compras { get; set; } = new List<Compra>();
}

public class Movimiento
{
    public string Fecha { get; set; } = string.Empty;
    public string Detalle { get; set; } = string.Empty;
    public string Importe { get; set; } = string.Empty;
}

public class MovimientosData
{
    public List<Movimiento> Movimientos { get; set; } = new List<Movimiento>();
}

public class MisDatos
{
    public string PersonaTipo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Cuil { get; set; } = string.Empty;
    public string Dni { get; set; } = string.Empty;
    public string Telefono1 { get; set; } = string.Empty;
    public string Telefono2 { get; set; } = string.Empty;
    public string Direccion1 { get; set; } = string.Empty;
    public string Direccion2 { get; set; } = string.Empty;
}
