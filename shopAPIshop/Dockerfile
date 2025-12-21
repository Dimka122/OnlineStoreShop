# Используем официальный образ .NET 8.0
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Этап сборки
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копируем csproj и восстанавливаем зависимости
COPY ["ECommerceShop.csproj", "."]
RUN dotnet restore "./ECommerceShop.csproj"

# Копируем все файлы проекта
COPY . .
WORKDIR "/src/."

# Публикуем приложение
RUN dotnet publish "ECommerceShop.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Финальный этап
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

# Создаем пользователя с ограниченными правами
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

# Запускаем приложение
ENTRYPOINT ["dotnet", "ECommerceShop.dll"]