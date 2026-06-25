# Демо Meerkatplace

## 1. Поднимите систему

```bash
docker-compose build
docker-compose up -d
```

## 2. Проверка здоровья сервисов

```bash
curl -s http://localhost:8080/health
curl -s http://localhost:5001/health
curl -s http://localhost:5002/health
curl -s http://localhost:5003/health
curl -s http://localhost:5004/health
```

## 3. Swagger UI

- Gateway: http://localhost:8080/docs
- User Service: http://localhost:5001/docs
- Listing Service: http://localhost:5002/docs
- Order Service: http://localhost:5003/docs
- Payment Service: http://localhost:5004/docs

## 4. Сквозной сценарий Register -> Checkout -> Paid

Ниже команды через Gateway (`http://localhost:8080`) с bearer-токеном.

### 4.1 Регистрация

```bash
curl -s -X POST http://localhost:8080/api/v1/auth/register \
  -H 'Content-Type: application/json' \
  -d '{
    "login": "demo_user_001",
    "email": "demo_user_001@example.com",
    "password": "Password123!"
  }'
```

### 4.2 Вход

```bash
curl -s -X POST http://localhost:8080/api/v1/auth/login \
  -H 'Content-Type: application/json' \
  -d '{
    "email": "demo_user_001@example.com",
    "password": "Password123!"
  }'
```

Сохраните `accessToken` из ответа.

### 4.3 Создание адреса пользователя

```bash
curl -s -X POST http://localhost:8080/api/v1/user/users/me/addresses \
  -H 'Content-Type: application/json' \
  -H 'Authorization: Bearer <accessToken>' \
  -d '{
    "country": "RU",
    "city": "Moscow",
    "street": "Tverskaya 1",
    "postalCode": "101000"
  }'
```

Сохраните `id` адреса как `addressId`.

### 4.4 Создание товара

```bash
curl -s -X POST http://localhost:8080/api/v1/listing/products \
  -H 'Content-Type: application/json' \
  -H 'Authorization: Bearer <accessToken>' \
  -d '{
    "name": "Demo Mouse",
    "description": "Demo product",
    "price": 1499.99,
    "currency": "RUB",
    "deliveryType": "Physical",
    "stockQuantity": 5
  }'
```

Сохраните `id` товара как `productId`.

### 4.5 Добавление товара в корзину

```bash
curl -s -X POST http://localhost:8080/api/v1/order/cart/items \
  -H 'Content-Type: application/json' \
  -H 'Authorization: Bearer <accessToken>' \
  -d '{
    "productId": "<productId>",
    "quantity": 1
  }'
```

### 4.6 Checkout

```bash
curl -s -X POST http://localhost:8080/api/v1/order/cart/checkout \
  -H 'Content-Type: application/json' \
  -H 'Authorization: Bearer <accessToken>' \
  -d '{
    "addressId": "<addressId>",
    "paymentMethod": "Wallet"
  }'
```

Сохраните `orderId`.

### 4.7 Проверка статуса заказа

```bash
curl -s -X GET http://localhost:8080/api/v1/order/orders/<orderId>/status \
  -H 'Authorization: Bearer <accessToken>'
```

Ожидаемая последовательность: `Placed -> Paid`.

### 4.8 Проверка отгрузки

```bash
curl -s -X GET http://localhost:8080/api/v1/order/orders/<orderId>/shipments \
  -H 'Authorization: Bearer <accessToken>'
```

Ожидается минимум одна запись для оплаченного заказа.

## 5. Негативный сценарий (недостаточно остатков)

1. Создайте товар со `stockQuantity = 1`.
2. Добавьте в корзину `quantity = 3`.
3. Оформите заказ.
4. Проверяйте `GET /api/v1/order/orders/<orderId>/status`.

Ожидаемое поведение: компенсирующий сценарий со статусом `Cancelled`.
