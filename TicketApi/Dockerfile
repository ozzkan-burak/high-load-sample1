# Build Aşaması (SDK ile kodu derle)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Önce proje dosyasını kopyala ve restore et
COPY TicketApi.csproj ./
RUN dotnet restore TicketApi.csproj

# Sonra tüm kaynak kodları kopyala
COPY . ./

# Projeyi belirterek publish et
RUN dotnet publish TicketApi.csproj -c Release -o out

# Runtime Aşaması (Sadece çalışma dosyalarını al - Hafif İmaj)
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .
EXPOSE 8080
ENTRYPOINT ["dotnet", "TicketApi.dll"]