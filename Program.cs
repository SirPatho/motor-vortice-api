using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using PKHeX.Core;
using System.IO;
using System.Text;
using System;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options => {
    options.AddPolicy("PermitirWeb", policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();
app.UseCors("PermitirWeb");

app.MapPost("/extraer-equipo", async (HttpRequest request) => {
    var form = await request.ReadFormAsync();
    var file = form.Files.GetFile("archivoSave");
    if (file == null) return Results.BadRequest("Archivo no recibido.");

    using var ms = new MemoryStream();
    await file.CopyToAsync(ms);
    byte[] data = ms.ToArray();

    SaveFile sav;
    try {
        // Forzamos la lectura como Rubí Omega / Zafiro Alfa
        sav = new SAV6AO(data);
    } catch {
        try {
            // Utilizamos la clase específica para Pokémon X / Y
            sav = new SAV6XY(data);
        } catch {
            return Results.BadRequest("Archivo incompatible o corrupto.");
        }
    }

    StringBuilder sb = new StringBuilder();
    
    // Extracción directa usando el índice validado por el motor
    for (int i = 0; i < sav.PartyCount; i++) {
        var pkmn = sav.GetPartySlotAtIndex(i);
        var showdownText = new ShowdownSet(pkmn).Text;
        sb.AppendLine(showdownText);
        sb.AppendLine();
    }

    return Results.Ok(sb.ToString().Trim());
});

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Run($"http://+:{port}");