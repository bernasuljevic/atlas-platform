# Build context REPO KÖKÜ olmalı (docker-compose.yml'deki "context: ." bunu
# sağlıyor) - Atlas.Api, src/ altındaki neredeyse tüm modüllere proje
# referansı veriyor, sadece kendi klasörünü kopyalamak yetmez.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/ src/

# .sln'e hiç ihtiyaç yok - dotnet publish, Atlas.Api.csproj'un ProjectReference
# zincirini kendisi takip edip ihtiyacı olan HER şeyi restore/derler. Test
# projelerini restore etmeye gerek kalmadığı için daha hızlı.
RUN dotnet publish src/Host/Atlas.Api/Atlas.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# .NET 8+ imajlarının varsayılanı 8080 (80 DEĞİL) - non-root kullanıcıyla
# çalışabilmek için. docker-compose.yml host tarafında 5000'e eşliyor.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Atlas.Api.dll"]
