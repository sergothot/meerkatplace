# Демо Meerkatplace

## 1. Откройте страницы документации

- Документация Gateway: http://localhost:8080/docs
- Документация сервиса пользователей: http://localhost:5001/docs
- Документация сервиса объявлений: http://localhost:5002/docs
- Документация сервиса заказов: http://localhost:5003/docs
- Документация сервиса платежей: http://localhost:5004/docs

## 2. Проверки здоровья сервисов

Выполните эти команды, чтобы показать готовность сервисов:

```bash
curl -s http://localhost:8080/health
curl -s http://localhost:5001/health
curl -s http://localhost:5002/health
curl -s http://localhost:5003/health
curl -s http://localhost:5004/health
```

## 3. Регистрация и вход через gateway

```bash
curl -s -X POST http://localhost:8080/api/v1/auth/register \
  -H 'Content-Type: application/json' \
  -d '{
    "login": "demo_user_001",
    "email": "demo_user_001@example.com",
    "password": "Password123!"
  }'
```

```bash
curl -s -X POST http://localhost:8080/api/v1/auth/login \
  -H 'Content-Type: application/json' \
  -d '{
    "email": "demo_user_001@example.com",
    "password": "Password123!"
  }'
```

Возьмите accessToken и user.id из ответа на вход.

## 4. Создайте товар в сервисе объявлений

```bash
curl -s -X POST http://localhost:8080/api/v1/listing/products \
  -H 'Content-Type: application/json' \
  -d '{
    "sellerId": "11111111-1111-1111-1111-111111111111",
    "name": "Demo Mouse",
    "description": "Demo product",
    "price": 1499.99,
    "currency": "RUB",
    "deliveryType": "Physical",
    "stockQuantity": 5
  }'
```

Сохраните id как productId.

## 5. Добавьте в корзину и оформите заказ

Используйте id аутентифицированного пользователя в заголовке X-User-Id.

```bash
curl -s -X POST http://localhost:8080/api/v1/order/cart/items \
  -H 'Content-Type: application/json' \
  -H 'Authorization: Bearer <accessToken>' \
  -H 'X-User-Id: <userId>' \
  -d '{
    "productId": "<productId>",
    "quantity": 1,
    "unitPrice": 1499.99,
    "currency": "RUB"
  }'
```

```bash
curl -s -X POST http://localhost:8080/api/v1/order/cart/checkout \
  -H 'Content-Type: application/json' \
  -H 'Authorization: Bearer <accessToken>' \
  -H 'X-User-Id: <userId>' \
  -d '{
    "addressId": "demo-address",
    "paymentMethod": "Wallet"
  }'
```

Сохраните orderId.

## 6. Проверяйте статус заказа

```bash
curl -s -X GET http://localhost:8080/api/v1/order/orders/<orderId>/status \
  -H 'Authorization: Bearer <accessToken>' \
  -H 'X-User-Id: <userId>'
```

Ожидаемая последовательность статусов: Placed -> Paid (успешный сценарий).

## 7. Покажите данные об отгрузке

```bash
curl -s -X GET http://localhost:8080/api/v1/order/orders/<orderId>/shipments \
  -H 'Authorization: Bearer <accessToken>' \
  -H 'X-User-Id: <userId>'
```

Ожидается: как минимум одна запись об отгрузке для оплаченного заказа.

## 8. Сценарий ошибки

- Создайте еще один товар с stockQuantity: 1.
- Добавьте в корзину количество 3.
- Оформите заказ снова.
- Проверяйте статус заказа, чтобы показать компенсирующее поведение (Cancelled).
