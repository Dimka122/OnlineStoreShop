# ECommerceShop API

ASP.NET Core Web API для e-commerce приложения с React фронтендом.

## Обзор

Это полноценный REST API для интернет-магазина, включающий:
- Аутентификацию и авторизацию пользователей (JWT)
- Управление товарами и категориями
- Корзину покупок
- Управление заказами
- Систему отзывов
- Административную панель

## Технологии

- **.NET 8.0**
- **ASP.NET Core Web API**
- **Entity Framework Core** (Code-First подход)
- **MySQL/MariaDB**
- **Identity Framework** (аутентификация и авторизация)
- **JWT Bearer** (токены аутентификации)
- **Swagger/OpenAPI** (документация API)
- **AutoMapper** (маппинг DTO)

## Установка и настройка

### Предварительные требования

- .NET 8.0 SDK
- MySQL/MariaDB
- Visual Studio 2022 или VS Code

### Настройка базы данных

1. Создайте базу данных MySQL:
```sql
CREATE DATABASE ECommerceShop;
```

2. Обновите строку подключения в `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=ECommerceShop;User=root;Password=your_password;Port=3306;"
  }
}
```

### Запуск приложения

1. Клонируйте репозиторий
2. Перейдите в папку проекта
3. Выполните команды:
```bash
dotnet restore
dotnet build
dotnet run
```

Приложение будет доступно по адресу: `https://localhost:7000`

## API Документация

Swagger документация доступна по адресу: `https://localhost:7000/swagger`

## Эндпоинты API

### Аутентификация (`/api/auth`)

#### POST `/api/auth/register`
Регистрация нового пользователя
```json
{
  "email": "user@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "password": "password123",
  "confirmPassword": "password123",
  "phoneNumber": "+1234567890",
  "address": "123 Main St",
  "city": "New York",
  "postalCode": "10001",
  "country": "USA"
}
```

#### POST `/api/auth/login`
Вход пользователя
```json
{
  "email": "user@example.com",
  "password": "password123"
}
```

#### GET `/api/auth/me`
Получение информации о текущем пользователе (требует авторизации)

#### PUT `/api/auth/profile`
Обновление профиля пользователя (требует авторизации)

### Товары (`/api/products`)

#### GET `/api/products`
Получение списка товаров с фильтрацией и пагинацией
```
GET /api/products?categoryId=1&search=laptop&page=1&pageSize=20&sortBy=price&sortOrder=asc
```

#### GET `/api/products/{id}`
Получение информации о товаре

#### GET `/api/products/{id}/related`
Получение связанных товаров

#### POST `/api/products`
Создание нового товара (требует роли Admin)

#### PUT `/api/products/{id}`
Обновление товара (требует роли Admin)

#### DELETE `/api/products/{id}`
Удаление товара (требует роли Admin)

### Категории (`/api/categories`)

#### GET `/api/categories`
Получение списка категорий

#### GET `/api/categories/{id}`
Получение информации о категории

#### GET `/api/categories/{id}/products`
Получение товаров категории

#### POST `/api/categories`
Создание новой категории (требует роли Admin)

#### PUT `/api/categories/{id}`
Обновление категории (требует роли Admin)

#### DELETE `/api/categories/{id}`
Удаление категории (требует роли Admin)

### Корзина (`/api/shoppingcart`)

#### GET `/api/shoppingcart`
Получение корзины текущего пользователя

#### POST `/api/shoppingcart/add`
Добавление товара в корзину
```json
{
  "productId": 1,
  "quantity": 2
}
```

#### PUT `/api/shoppingcart/items/{itemId}`
Обновление количества товара в корзине
```json
{
  "quantity": 3
}
```

#### DELETE `/api/shoppingcart/items/{itemId}`
Удаление товара из корзины

#### DELETE `/api/shoppingcart/clear`
Очистка корзины

### Заказы (`/api/orders`)

#### GET `/api/orders`
Получение истории заказов пользователя

#### GET `/api/orders/{id}`
Получение деталей заказа

#### POST `/api/orders`
Создание нового заказа
```json
{
  "shippingAddress": "123 Main St",
  "shippingCity": "New York",
  "shippingPostalCode": "10001",
  "shippingCountry": "USA",
  "phoneNumber": "+1234567890",
  "notes": "Please deliver after 5 PM"
}
```

#### POST `/api/orders/{id}/cancel`
Отмена заказа

### Административные эндпоинты (`/api/admin`)

#### GET `/api/admin/dashboard`
Получение статистики для дашборда

#### GET `/api/admin/users`
Получение списка пользователей

#### GET `/api/admin/users/{id}`
Получение информации о пользователе

#### POST `/api/admin/users/{id}/lock`
Блокировка пользователя

#### POST `/api/admin/users/{id}/unlock`
Разблокировка пользователя

#### GET `/api/admin/analytics`
Получение аналитической информации

#### GET `/api/admin/settings`
Получение настроек системы

## Модели данных

### Пользователь (User)
- Id, Email, FirstName, LastName
- PhoneNumber, Address, City, Country
- Роли (User, Admin)

### Товар (Product)
- Id, Name, Description, Price, SalePrice
- StockQuantity, ImageUrl, IsActive, IsFeatured
- Category (связь с категорией)

### Категория (Category)
- Id, Name, Description, ImageUrl
- IsActive, Products (коллекция товаров)

### Заказ (Order)
- Id, OrderNumber, TotalAmount, TaxAmount, ShippingAmount
- ShippingAddress, Status, OrderDate
- User (связь с пользователем), OrderItems (коллекция)

### Отзыв (ProductReview)
- Id, ProductId, UserId, Rating, Comment
- CreatedAt, IsApproved

## Авторизация

API использует JWT Bearer токены для авторизации. Для доступа к защищенным эндпоинтам:

1. Получите токен через эндпоинт `/api/auth/login`
2. Добавьте токен в заголовок запроса:
```
Authorization: Bearer your_jwt_token_here
```

## Роли пользователей

- **User**: Обычный пользователь (доступ к корзине, заказам, профилю)
- **Admin**: Администратор (полный доступ к системе)

## Статусы заказов

- `Pending` - В обработке
- `Processing` - Обрабатывается
- `Shipped` - Отправлен
- `Delivered` - Доставлен
- `Cancelled` - Отменен
- `Refunded` - Возвращен

## Обработка ошибок

API возвращает стандартизированные ошибки:
```json
{
  "message": "Error description",
  "statusCode": 400,
  "timestamp": "2024-01-01T12:00:00Z"
}
```

## Лимиты и пагинация

Большинство GET эндпоинтов поддерживают пагинацию:
- `page` - номер страницы (по умолчанию: 1)
- `pageSize` - размер страницы (по умолчанию: 20, максимум: 100)

## Фильтрация и сортировка

Эндпоинты товаров поддерживают:
- `categoryId` - фильтрация по категории
- `search` - поиск по названию и описанию
- `isFeatured` - только избранные товары
- `onSale` - только товары со скидкой
- `sortBy` - поле сортировки (name, price, createdAt)
- `sortOrder` - порядок сортировки (asc, desc)

## Безопасность

- Все пароли хешируются
- JWT токены имеют ограниченное время жизни
- Защита от CSRF атак
- Валидация всех входных данных
- Rate limiting для эндпоинтов

## Логирование

- Все запросы логируруются
- Медленные запросы (>1000ms) помечаются отдельно
- Ошибки и исключения подробно логируются
- Аудит действий администраторов

## Docker

Для запуска в Docker:
```bash
docker build -t ecommerce-api .
docker run -p 7000:8080 ecommerce-api
```

## Тестирование

API включает unit и integration тесты. Запуск тестов:
```bash
dotnet test
```

## Лицензия

MIT License