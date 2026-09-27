# Руководство по подготовке к защите проекта и техническому собеседованию: TalentHub (C# / .NET 10 / React)

Данный документ — это исчерпывающее практическое руководство для успешной сдачи защиты курсового/дипломного проекта и прохождения собеседований на позицию C# / .NET разработчика. Вся теория подкреплена реальным кодом нашего проекта **TalentHub**, детально разобрана каждая непонятная строка синтаксиса, анатомия миграций, EF Core, LINQ, DI, безопасности и работы с облачным S3-хранилищем.

---

## 📌 Подробное содержание

1. [Архитектура и устройство ключевых фич («Где и как это сделано»)]
   - 1.1. [Program.cs — Анатомия точки входа и конвейера обработки запросов (Middleware Pipeline)]
   - 1.2. [Модуль Аутентификации и Авторизации (Auth & Identity)]
   - 1.3. [Модуль Резюме и Динамических Атрибутов (CV & EAV Pattern)]
   - 1.4. [Модуль Вакансий и Правил Доступа (Positions & Matching Rules)]
   - 1.5. [Модуль Хранения Файлов (Cloudflare R2 Object Storage / S3)]
2. [Разбор синтаксиса и фундаментальных концепций C#]
   - 2.1. [Что такое Guid и почему не int / long?]
   - 2.2. [Что такое record и чем он отличается от class?]
   - 2.3. [Автосвойства и инициализация: public string Name { get; set; } = string.Empty;]
   - 2.4. [Анатомия сложной сигнатуры: Task<ActionResult<PagedResult<AdminUserView>>>]
   - 2.5. [Построчный разбор метода контроллера: CancellationToken, async/await, Ok()]
   - 2.6. [Атрибуты контроллеров: [ApiController], [Authorize], [Route], [FromServices]]
3. [Устройство слоя данных (/Data), Entity Framework Core 10 и LINQ]
   - 3.1. [Что находится в папке /Data?]
   - 3.2. [Что такое LINQ и как C# запросы превращаются в SQL (IQueryable vs IEnumerable)]
   - 3.3. [Ключевые методы LINQ с примерами из нашего проекта]
   - 3.4. [Оптимизация: Change Tracker, AsNoTracking и проблема N+1]
4. [Миграции базы данных и анатомия сгенерированного кода EF Core]
   - 4.1. [Где и как создаются и применяются миграции?]
   - 4.2. [Построчный разбор аннотаций модели и колонок IdentityRole]
   - 4.3. [Полнотекстовый поиск PostgreSQL: tsvector, NpgsqlTsVector и английский стемминг]
5. [Разбор сидирования данных (DevelopmentDataSeeder & DatabaseSeeder)]
   - 5.1. [Зачем IServiceProvider.CreateScope() при старте?]
   - 5.2. [Построчный разбор EnsureAttrAsync и предотвращение дубликатов]
   - 5.3. [Требования курса: Dropdown "CAP" в категории "Certificates" и другие атрибуты]
6. [Топ вопросов комиссии с готовыми эталонными ответами]
7. [Live-Coding на защите: «Открой файл X и добавь фичу Y»]
8. [Шпаргалка терминов и психологические советы для уверенной защиты]

---

## 1. Архитектура и устройство ключевых фич («Где и как это сделано»)

### 1.1. Program.cs — Анатомия точки входа и конвейера обработки запросов (Middleware Pipeline)

Файл: `backend/backend/Program.cs`  
**Что это такое:** Главная точка входа приложения в .NET (используется современный синтаксис Top-Level Statements C# 10+ без лишней обертки `class Program { static void Main() }`).

#### 1. Секция регистрации сервисов (DI Контейнер — `builder.Services`):
В этой секции мы регистрируем зависимости и задаем им времена жизни (`Lifetime`):
```csharp
// 1. Подключение PostgreSQL через EF Core
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// 2. Подключение ASP.NET Core Identity (пользователи, роли, хеширование паролей)
builder.Services.AddIdentity<AppUser, IdentityRole<Guid>>(options => {
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// 3. Регистрация бизнес-сервисов (Scoped — живут в рамках одного HTTP-запроса)
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<IFileStorageService, CloudflareR2StorageService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IPositionService, PositionService>();
builder.Services.AddScoped<ICvService, CvService>();
```

#### 2. Секция Middleware (Конвейер обработки HTTP-запроса — `app.Use...`):
> **Критически важный вопрос комиссии:** *«Имеет ли значение порядок вызова методов Use... в Program.cs?»*  
> **Ответ:** **Да, порядок имеет решающее значение!** Каждый Middleware передает управление следующему через `next()`. Если поменять местами `UseAuthentication` и `UseAuthorization`, авторизация попытается проверить роли ДО того, как система расшифрует JWT-токен и узнает, кто совершает запрос!

Правильный порядок в нашем проекте:
1. `app.UseForwardedHeaders()` — извлекает реальный IP клиента и протокол HTTPS от обратного прокси Render/Nginx.
2. `app.UseCors("frontend")` — проверяет заголовки браузера и разрешает запросы с домена нашего фронтенда (`cvsystem-frontend.onrender.com`).
3. `app.UseMiddleware<ExceptionHandlingMiddleware>()` — глобальный перехватчик всех необработанных исключений (`try/catch`). Если в сервисе выбросился `NotFoundException`, мидлварь ловит его и возвращает чистый JSON с кодом 404, не роняя сервер.
4. `app.UseAuthentication()` — проверяет заголовок `Authorization: Bearer <token>`, валидирует криптографическую подпись JWT и создает объект `ClaimsPrincipal` (`User`).
5. `app.UseAuthorization()` — проверяет, имеет ли данный пользователь доступ к эндпоинту (по атрибутам `[Authorize]` или `[Authorize(Roles = "Administrator")]`).
6. `app.MapControllers()` — направляет проверенный запрос в соответствующий метод контроллера.
7. `app.MapGet("/api/health", ...)` — легковесный эндпоинт для проверки здоровья сервера и предотвращения засыпания (Keep-Alive).

---

### 1.2. Модуль Аутентификации и Авторизации (Auth & Identity)

* **Файлы:**
  - Контроллер: `backend/backend/Controllers/AuthController.cs`
  - Сервис: `backend/backend/Services/AuthService.cs`
  - Генерация токенов: `backend/backend/Auth/JwtTokenService.cs`
  - Текущий пользователь: `backend/backend/Auth/CurrentUserService.cs`
  - Сущность: `backend/backend/Entities/AppUser.cs`

#### Как происходит процесс входа (Login):
1. Клиент отправляет `POST /api/auth/login` с JSON `{ email, password }`.
2. Контроллер вызывает `authService.LoginAsync(request)`.
3. Сервис находит пользователя в БД: `await userManager.FindByEmailAsync(request.Email)`.
4. Проверяет пароль через криптографический хешер: `await userManager.CheckPasswordAsync(user, request.Password)`. Пароли хешируются алгоритмом **PBKDF2 с солью** (Salt).
5. Проверяет, не заблокирован ли пользователь (`user.IsBlocked`).
6. `JwtTokenService.CreateToken(user, roles)` генерирует подписанную строку JWT.

#### Что внутри нашего JWT-токена (Claims):
- `ClaimTypes.NameIdentifier` (`sub`) — уникальный `Guid` пользователя.
- `ClaimTypes.Email` — адрес электронной почты.
- `ClaimTypes.Role` — список ролей (`Candidate`, `Recruiter`, `Administrator`).
- `auth_version` — номер версии авторизации (`user.AuthVersion`).

#### Как устроен механизм отзыва токенов (Logout со всех устройств):
В классическом JWT токен автономен (stateless), его нельзя отозвать до истечения `exp`.  
**Наше архитектурное решение:** в сущность `AppUser` добавлено целочисленное поле `AuthVersion`.
- При вызове «Выйти со всех устройств» или смене пароля сервер делает: `user.AuthVersion++`.
- При каждом запросе `CurrentUserService` проверяет совпадение версии из токена с версией пользователя в БД. Если версия в токене меньше — запрос отклоняется со статусом 401 Unauthorized.

---

### 1.3. Модуль Резюме и Динамических Атрибутов (CV & EAV Pattern)

* **Файлы:**
  - Контроллер: `backend/backend/Controllers/CvsController.cs`
  - Сервис: `backend/backend/Services/CvService.cs`
  - Сущности: `backend/backend/Entities/AttributeDefinition.cs`, `AttributeOption.cs`, `UserAttributeValue.cs`
  - Хелпер валидации: `backend/backend/Services/AttributeValueHelper.cs`

#### Архитектурный паттерн EAV (Entity-Attribute-Value):
По требованиям проекта резюме должно содержать произвольные настраиваемые поля: выпадающие списки (CAP, English Level), числа (GPA), даты (Period), логические переключатели (Python, Apache Hadoop), изображения (сертификаты).
Если бы мы создавали отдельную колонку в таблице пользователей под каждое поле, то при добавлении нового сертификата пришлось бы пересоздавать структуру базы данных (делать миграцию).
Вместо этого мы применили паттерн **EAV**:
1. `AttributeDefinition` — описывает метаданные атрибута (Имя: "CAP", Категория: "Certificates", Тип: `Dropdown`, Описание).
2. `AttributeOption` — варианты для выпадающего списка ("None", "Essentials", "Pro", "Expert").
3. `UserAttributeValue` — конкретное значение, заполненное кандидатом:
   - `TextValue` — для строковых и текстовых значений.
   - `NumberValue` — для числовых показателей (например, GPA 3.8).
   - `BooleanValue` — для флажков да/нет.
   - `OptionId` — внешний ключ на выбранный вариант из `AttributeOption`.
   - `PeriodStart` / `PeriodEnd` — временные интервалы.
   - `ImageObjectKey` — ключ загруженного сертификата в S3.

---

### 1.4. Модуль Вакансий и Правил Доступа (Positions & Matching Rules)

* **Файлы:**
  - Контроллер: `backend/backend/Controllers/PositionsController.cs`
  - Сервис: `backend/backend/Services/PositionService.cs`
  - Сущности: `Position.cs`, `PositionAccessRule.cs`
  - Движок матчинга: `backend/backend/Services/PositionAccessHelper.cs`

#### Как работает сопоставление кандидата с вакансией:
Рекрутер задает правила для вакансии: например, «English Level должен быть C1 или C2» и «GPA > 3.5».
1. Каждое правило хранится в `PositionAccessRule` (ссылка на `AttributeId`, оператор сравнения `Operator`, требуемое значение).
2. Поддерживаемые операторы: `Equals`, `NotEquals`, `GreaterThan`, `LessThan`, `Contains`.
3. При открытии вакансии или списка откликов `PositionAccessHelper.Matches(userValues, rules)` проверяет каждое правило.
4. В сводной таблице резюме (`Matrix Table`) формируется матрица кандидатов, где зеленым подсвечиваются выполненные требования, а красным — несовпадения.

---

### 1.5. Модуль Хранения Файлов (Cloudflare R2 Object Storage / S3)

* **Файлы:**
  - Контроллер: `backend/backend/Controllers/FilesController.cs`
  - Сервис: `backend/backend/Storage/CloudflareR2StorageService.cs`
  - Интерфейс: `backend/backend/Services/Interfaces/IFileStorageService.cs`

#### Почему Cloudflare R2 вместо папки на диске или базы данных:
1. **Эфемерность контейнеров Render/Docker**: при каждом перезапуске контейнера локальные файлы на диске стираются. Внешнее S3-хранилище сохраняет файлы навсегда.
2. **База данных не раздувается**: сохранение тяжелых картинок в PostgreSQL замедляет резервное копирование и забивает оперативную память буферного пула.
3. **Бесплатный исходящий трафик (Egress-Free)**: Cloudflare R2 в отличие от Amazon AWS S3 не берет денег за раздачу скачиваемых файлов через сеть.
4. **CDN**: файлы кэшируются на ближайших к пользователю серверах Cloudflare по всему миру.

#### Механизм загрузки (Direct Streaming):
1. Фронтенд шлет файл через `multipart/form-data` на `POST /api/files/upload`.
2. Бэкенд валидирует MIME-тип (только `image/jpeg`, `image/png`, `image/webp`) и размер (до 5 МБ).
3. Поток данных (`Stream`) через `AmazonS3Client.PutObjectAsync` потоково передается в Cloudflare R2 без сохранения на локальный жесткий диск сервера.
4. Генерируется уникальный ключ: `users/{userId}/{guid}.jpg`.
5. Пользователю отдается готовая ссылка на публичный CDN: `https://pub-1834497bb7c24e8cb36a4a06dd7e7ffd.r2.dev/users/.../....jpg`.

---

## 2. Разбор синтаксиса и фундаментальных концепций C#

### 2.1. Что такое Guid и почему не int / long?

```csharp
public Guid Id { get; set; }
```
* **Что такое Guid (Globally Unique Identifier):** 128-битное целое число (16 байт), записываемое в виде 32 шестнадцатеричных цифр, разделенных дефисами: `d7346498-df28-4b36-a269-e0cc1a0fae0c`. В PostgreSQL этот тип называется `uuid`.
* **Почему мы используем Guid вместо автоинкрементного `int (1, 2, 3...)`:**
  1. **Безопасность (ID Enumeration Attack):** если использовать `int`, злоумышленник может по очереди дергать `/api/cvs/1`, `/api/cvs/2`, `/api/cvs/3` и выкачать всю базу. С `Guid` угадать чужой идентификатор математически невозможно (вероятность коллизии стремится к абсолютному нулю).
  2. **Генерация на клиенте/бэкенде без обращения к БД:** мы можем вызвать `Guid.NewGuid()` прямо в коде C# до сохранения в базу и сразу связать зависимые сущности. С `int` пришлось бы делать вставку, ждать генерации ID базой данных и только потом вставлять дочерние строки.
  3. **Слияние и репликация данных:** если базы данных объединяются из разных филиалов, идентификаторы `Guid` гарантированно никогда не пересекутся.

---

### 2.2. Что такое record и чем он отличается от class?

В наших DTO мы пишем:
```csharp
public record PositionListItem(
    Guid Id,
    string Title,
    string? Company,
    string? Level,
    DateTime UpdatedAt,
    bool IsPublic,
    int CvsCount
);
```

* **Главные отличия `record` от `class`:**
  1. **Сравнение по значениям (Value-based equality):**
     Два экземпляра обычного `class` с одинаковыми полями при проверке через `==` вернут `false`, потому что класс сравнивает **ссылки на область памяти**.
     Экземпляры `record` сравнивают **значения всех полей** — если все поля равны, то `rec1 == rec2` вернет `true`.
  2. **Иммутабельность (Неизменяемость):**
     При позиционном синтаксисе свойства автоматически становятся `{ get; init; }`. После создания объект нельзя случайно модифицировать из другого потока.
  3. **Оператор `with` (Неразрушающая мутация):**
     `var updated = item with { Title = "Senior Lead" };` создает копию с измененным полем, оставляя оригинал неизменным.
  4. **Автогенерация кода компилятором:**
     Компилятор автоматически генерирует методы `Equals()`, `GetHashCode()`, деконструктор и понятный вывод в `ToString()`.
* **Почему `record` идеален для DTO:** DTO — это просто контейнер данных без сложного поведения. Нам важна неизменяемость и предсказуемое сравнение.

---

### 2.3. Автосвойства и инициализация: public string Name { get; set; } = string.Empty;

```csharp
public string Name { get; set; } = string.Empty;
```
Разберем каждую часть этой конструкции:
1. **`public`** — модификатор доступа: свойство доступно из любого места программы.
2. **`string`** — тип данных (строка). Заметьте: без вопросительного знака (`string?`), то есть поле **Non-Nullable** (не может содержать `null`).
3. **`Name`** — имя свойства (по соглашению C# пишется в PascalCase).
4. **`{ get; set; }`** — **автоматическое свойство (Auto-implemented Property)**. Компилятор сам неявно создаст за кулисами скрытое приватное поле (`backing field`), а `get` и `set` будут методами для чтения и записи. Это инкапсуляция: если позже понадобится добавить валидацию в сеттер, сигнатура класса не изменится.
5. **`= string.Empty;`** — инициализатор значения по умолчанию. Начиная с C# 8 в .NET включена функция **Nullable Reference Types**. Если мы объявим `string Name;` без знака `?` и не инициализируем его, компилятор выдаст предупреждение: *«Возможный null при разыменовании»*. Присваивание `string.Empty` гарантирует, что свойство всегда содержит пустую строку `""`, а не коварный `null`, защищая от сбоев `NullReferenceException`.

---

### 2.4. Анатомия сложной сигнатуры: Task<ActionResult<PagedResult<AdminUserView>>>

```csharp
public async Task<ActionResult<PagedResult<AdminUserView>>> ListUsers(...)
```
Эта конструкция выглядит сложно, но устроена как матрешка, где каждый слой решает конкретную инженерную задачу:

```text
Task<
    ActionResult<
        PagedResult<
            AdminUserView
        >
    >
>
```

1. **`AdminUserView`** (внутренний слой) — это **DTO** (Data Transfer Object). Описывает, какие конкретно поля пользователя увидит администратор в таблице (Id, Email, ФИО, Роли, Статус блокировки, Дата создания). Сюда НЕ входят хеш пароля и секретные поля.
2. **`PagedResult<T>`** (второй слой) — структура **пагинации**. Содержит:
   - `Items: IReadOnlyList<T>` — список элементов текущей страницы (например, 10 пользователей).
   - `TotalCount: int` — сколько всего записей в БД (например, 154).
   - `Page: int` — номер текущей страницы (1).
   - `PageSize: int` — сколько на странице (10).
   - `TotalPages: int` — общее число страниц (16).
3. **`ActionResult<T>`** (третий слой) — специальный тип ASP.NET Core. Он позволяет методу контроллера вернуть **либо** строго типизированный объект (`return Ok(pagedResult)`), **либо** стандартный HTTP-ответ с кодом ошибки (`return NotFound()`, `return BadRequest("Invalid filter")`). Без `ActionResult<T>` Swagger не смог бы автоматически сгенерировать точную схему API.
4. **`Task<T>`** (внешний слой) — объект асинхронной операции из TPL (Task Parallel Library). Указывает, что метод выполняется **асинхронно** и не блокирует поток ОС во время ожидания ответа от базы данных.

---

### 2.5. Построчный разбор метода контроллера: CancellationToken, async/await, Ok()

Возьмем фрагмент, о котором спрашивал пользователь:
```csharp
public async Task<ActionResult<PagedResult<AdminUserView>>> List(
    [FromQuery] string? q,
    [FromQuery] string? role,
    [FromQuery] bool? isBlocked,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10,
    CancellationToken cancellationToken = default) {
    
    var result = await adminUserService.ListUsersAsync(q, role, isBlocked, page, pageSize, cancellationToken);
    return Ok(result);
}
```

* **`[FromQuery] string? q`** — модель-байндер ASP.NET Core автоматически берет значение параметра из URL-строки запроса (например: `/api/admin/users?q=ivan&page=2`). Знак `?` означает, что параметр не обязателен (может быть `null`).
* **`int page = 1`** — значение по умолчанию. Если клиент не передал параметр в URL, он будет равен 1.
* **`CancellationToken cancellationToken = default`**:
  - `CancellationToken` — это структурный токен отмены.
  - `default` — синтаксис C#, подставляющий пустое значение `CancellationToken.None`, если токен не передан явно.
  - **Как это работает в реальности:** когда пользователь открывает страницу в браузере, отправляется запрос. Если пользователь резко закрывает вкладку или переходит на другую страницу, браузер обрывает TCP-соединение. Kestrel (веб-сервер .NET) переводит `cancellationToken` в состояние «Отменено». Метод EF Core немедленно посылает сигнал PostgreSQL прервать выполнение тяжелого SQL-запроса, экономя такты процессора и память сервера!
* **`var result = await adminUserService.ListUsersAsync(...);`**:
  - `await` — ключевое слово асинхронности. Оно освобождает текущий поток пула потоков (`ThreadPool`) обратно серверу для обслуживания других входящих клиентов, пока PostgreSQL выполняет поиск. Когда база присылает ответ, поток из пула подхватывает выполнение кода со следующей строки.
* **`return Ok(result);`**:
  - Метод базового класса `ControllerBase`. Оборачивает данные в HTTP-ответ со статус-кодом **200 OK** и сериализует `result` в формат JSON.

---

### 2.6. Атрибуты контроллеров: [ApiController], [Authorize], [Route], [FromServices]

```csharp
[ApiController]
[Authorize]
[Route("api/profile")]
public class ProfileController : ControllerBase {
    [HttpPost("avatar")]
    public async Task<IActionResult> UploadAvatar(
        [FromServices] UserManager<AppUser> userManager,
        CancellationToken cancellationToken) { ... }
}
```

1. **`[ApiController]`**:
   - Включает автоматическую валидацию модели: если в запросе переданы некорректные данные, контроллер сам вернет HTTP 400 Bad Request с описанием ошибок в стандарте RFC 7807 (ProblemDetails), даже не заходя в тело метода!
   - Автоматически выбирает источник привязки параметров (`[FromBody]` для сложных объектов, `[FromRoute]` для параметров пути).
2. **`[Authorize]`**:
   - Защищает весь контроллер или отдельный метод. Если в запросе нет валидного JWT-токена, ASP.NET Core автоматически отсечет запрос с кодом **401 Unauthorized**.
   - Можно указать роли: `[Authorize(Roles = "Administrator")]` — вернет **403 Forbidden**, если токен есть, но у пользователя нет нужной роли.
3. **`[Route("api/profile")]`**:
   - Задает шаблон URL-маршрутизации. Все методы внутри этого контроллера будут начинаться с `/api/profile`.
4. **`[FromServices] UserManager<AppUser> userManager`**:
   - **Внедрение зависимости в конкретный метод действия (Action Injection)**.
   - Обычно зависимости внедряются через конструктор класса. Но если тяжелый сервис нужен только в одном-единственном методе контроллера из десяти, внедрение через `[FromServices]` позволяет не создавать и не резолвить его при вызове других методов.

---

## 3. Устройство слоя данных (/Data), Entity Framework Core 10 и LINQ

### 3.1. Что находится в папке /Data?

Папка: `backend/backend/Data/`  
1. **`AppDbContext.cs`**: Главный контекст базы данных. Наследуется от `IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>`. Он объединяет стандартные таблицы авторизации (`AspNetUsers`, `AspNetRoles`) с нашими доменными сущностями (`Positions`, `Cvs`, `Attributes`, `Discussions` и т.д.).
2. **`Configurations/` (Fluent API)**:
   - `AppUserConfiguration.cs`, `PositionConfiguration.cs`, `CvConfiguration.cs` и др.
   - Вместо того чтобы захламлять классы сущностей атрибутами `[MaxLength(200)]`, `[Required]`, мы выносим настройки колонок, связей и внешних ключей в отдельные конфигурационные классы, реализующие `IEntityTypeConfiguration<T>`.
   - В `AppDbContext` они регистрируются одной строкой: `modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);`.
3. **`Migrations/`**: Файлы сгенерированных миграций, отражающие историю эволюции схемы БД.
4. **`Seed/`**:
   - `DatabaseSeeder.cs` — начальное создание системных ролей (`Candidate`, `Recruiter`, `Administrator`) и дефолтного администратора.
   - `DevelopmentDataSeeder.cs` — тестовое наполнение вакансиями, кандидатами и предопределенными атрибутами (CAP, English Level, GPA).

---

### 3.2. Что такое LINQ и как C# запросы превращаются в SQL (IQueryable vs IEnumerable)

**LINQ (Language Integrated Query)** — язык запросов, встроенный прямо в синтаксис C#.

> **Главный вопрос на собеседовании:** *«В чем принципиальная разница между IQueryable<T> и IEnumerable<T>?»*

```csharp
// ПРИМЕР 1: IQueryable (Запрос выполняется в PostgreSQL)
IQueryable<Position> query = db.Positions
    .Where(p => p.IsPublic && p.Level == "Senior");
var result = await query.ToListAsync();

// ПРИМЕР 2: IEnumerable (Запрос выполняется в памяти C#)
IEnumerable<Position> memoryList = db.Positions.ToList(); // Выкачали ВСЕ записи из БД в RAM!
var filtered = memoryList.Where(p => p.IsPublic && p.Level == "Senior");
```

* **Как работает `IQueryable`:**
  Когда мы пишем `.Where(p => p.IsPublic)`, метод не фильтрует объекты прямо сейчас. Он строит **дерево выражений (Expression Tree)**.
  Провайдер Npgsql (PostgreSQL) анализирует это дерево и переводит его в чистый SQL:
  ```sql
  SELECT p."Id", p."Title", p."Level" 
  FROM "Positions" AS p 
  WHERE p."IsPublic" = TRUE AND p."Level" = 'Senior';
  ```
  База данных выполняет поиск по индексам и возвращает в C# только нужные 5 строк!
* **Что делает `IEnumerable`:**
  Если вызвать `.ToList()` до фильтрации, EF Core выполнит `SELECT * FROM "Positions"` и загрузит миллион строк в оперативную память сервера. Только после этого C# в цикле начнет отбирать нужные. Это приводит к задержкам и ошибке `OutOfMemoryException`.

---

### 3.3. Ключевые методы LINQ с примерами из нашего проекта

1. **`.Where(predicate)`** — фильтрация (SQL `WHERE`):
   ```csharp
   query = query.Where(cv => cv.Status == CvStatus.Published);
   ```
2. **`.Select(projection)`** — проекция в DTO (SQL `SELECT specific_columns`):
   Позволяет читать из базы только нужные колонки, а не тянуть всю таблицу:
   ```csharp
   .Select(p => new PositionTitleDto(p.Id, p.Title))
   ```
3. **`.Include()` и `.ThenInclude()`** — жадная загрузка связанных таблиц (SQL `JOIN`):
   ```csharp
   var position = await db.Positions
       .Include(p => p.AccessRules)
           .ThenInclude(r => r.Attribute)
       .FirstOrDefaultAsync(p => p.Id == id);
   ```
4. **`.OrderBy()` / `.OrderByDescending()`** — сортировка (SQL `ORDER BY`):
   ```csharp
   .OrderByDescending(p => p.UpdatedAt)
   ```
5. **`.Skip(count)` и `.Take(count)`** — пагинация (SQL `OFFSET / LIMIT`):
   ```csharp
   .Skip((page - 1) * pageSize).Take(pageSize)
   ```
6. **`.AnyAsync()`** — проверка наличия хотя бы одной записи (SQL `SELECT EXISTS(...)`):
   В отличие от `.CountAsync() > 0`, метод `AnyAsync` останавливается сразу при нахождении первой совпавшей строки, что в разы быстрее.

---

### 3.4. Оптимизация: Change Tracker, AsNoTracking и проблема N+1

1. **Что такое Change Tracker:**
   Когда вы запрашиваете сущность из БД: `var user = await db.Users.FindAsync(id);`, EF Core создает снимок ее исходного состояния. Когда вы меняете свойство `user.FirstName = "Alex";` и вызываете `db.SaveChangesAsync()`, трекер сравнивает текущее состояние со снимком и генерирует `UPDATE` только для изменившихся колонок.
2. **Зачем нужен `.AsNoTracking()`:**
   Для операций **только для чтения (Read-Only)** отслеживание изменений не нужно:
   ```csharp
   var list = await db.Positions.AsNoTracking().ToListAsync();
   ```
   Это экономит до 50% оперативной памяти и существенно ускоряет работу приложения.
3. **Проблема N+1:**
   Если вы загрузите 100 вакансий, а затем в цикле `foreach` начнете обращаться к `position.Cvs`, EF Core выполнит 1 запрос за вакансиями и еще 100 отдельных SQL-запросов за резюме каждой вакансии (всего 101 запрос!).  
   **Решение:** использовать `.Include(p => p.Cvs)` или формировать проекцию через `.Select()`.

---

## 4. Миграции базы данных и анатомия сгенерированного кода EF Core

### 4.1. Где и как создаются и применяются миграции?

* **Что такое миграции:** Механизм EF Core для версионирования схемы базы данных с помощью кода C#.
* **Команды в консоли:**
  - Создать миграцию: `dotnet ef migrations add InitialCreate`
  - Обновить базу вручную: `dotnet ef database update`
  - Удалить последнюю непримененную: `dotnet ef migrations remove`
* **Автоматическое применение в коде при запуске:**
  В файле `DatabaseSeeder.cs` первой строчкой выполняется:
  ```csharp
  await db.Database.MigrateAsync();
  ```
  EF Core проверяет специальную таблицу `__EFMigrationsHistory` в PostgreSQL. Если там нет записи о какой-то миграции, он накатывает недостающие SQL-скрипты автоматически прямо при старте контейнера в Render/Docker!

---

### 4.2. Построчный разбор аннотаций модели и колонок IdentityRole

Разберем фрагмент из файла модели EF Core, о котором спрашивал пользователь:

```csharp
.HasAnnotation("ProductVersion", "10.0.12")
.HasAnnotation("Relational:MaxIdentifierLength", 63);

NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRole<System.Guid>", b =>
    {
        b.Property<Guid>("Id")
            .ValueGeneratedOnAdd()
            .HasColumnType("uuid");

        b.Property<string>("ConcurrencyStamp")
            .IsConcurrencyToken()
            .HasColumnType("text");

        b.Property<string>("Name")
            .HasMaxLength(256)
            .HasColumnType("character varying(256)");

        b.Property<string>("NormalizedName")
            .HasMaxLength(256)
            .HasColumnType("character varying(256)");

        b.HasKey("Id");

        b.HasIndex("NormalizedName")
            .IsUnique()
            .HasDatabaseName("RoleNameIndex");

        b.ToTable("AspNetRoles", (string)null);
    });
```

* **`HasAnnotation("Relational:MaxIdentifierLength", 63)`**:
  СУБД PostgreSQL имеет жесткое системное ограничение: имя любой таблицы, колонки или индекса не может превышать **63 байта**. EF Core автоматически обрезает длинные имена, чтобы они не вызывали ошибок в Postgres.
* **`UseIdentityByDefaultColumns(modelBuilder)`**:
  Указывает провайдеру Npgsql использовать стандартный SQL-синтаксис `GENERATED BY DEFAULT AS IDENTITY` вместо устаревших последовательностей `SERIAL`.
* **`b.Property<Guid>("Id").HasColumnType("uuid")`**:
  Сопоставляет C# тип `Guid` с нативным 16-байтным типом `uuid` в PostgreSQL.
* **`b.Property<string>("ConcurrencyStamp").IsConcurrencyToken()`**:
  **Токен параллелизма (Optimistic Concurrency)**. При каждом изменении роли этот токен перезаписывается случайной строкой (`Guid.NewGuid().ToString()`). Если два администратора одновременно попытаются отредактировать одну и ту же роль, второй получит `DbUpdateConcurrencyException`, что предотвратит случайную перезапись чужих изменений.
* **`NormalizedName` и индекс `RoleNameIndex`**:
  В Identity имя роли хранится в двух колонках: `Name` ("Administrator") и `NormalizedName` ("ADMINISTRATOR" — в верхнем регистре). Поиск ролей всегда ведется по `NormalizedName` с уникальным индексом, благодаря чему проверка ролей не зависит от регистра букв и выполняется за миллисекунды.

---

### 4.3. Полнотекстовый поиск PostgreSQL: tsvector, NpgsqlTsVector и английский стемминг

Разберем фрагмент полнотекстового поиска:
```csharp
name: "SearchVector",
table: "Positions",
type: "tsvector",
nullable: false)
.Annotation("Npgsql:TsVectorConfig", "english")
.Annotation("Npgsql:TsVectorProperties", new[] { "Title", "ShortDescription" });

migrationBuilder.AddColumn<NpgsqlTsVector>(
    name: "SearchVector",
    table: "AspNetUsers",
    type: "tsvector",
    nullable: false);
```

* **Что такое `tsvector` в PostgreSQL:**
  Это специализированный тип данных для **полнотекстового поиска (Full-Text Search)**. Обычный поиск через `LIKE '%developer%'` делает полное сканирование таблицы (Full Table Scan) и дико тормозит на больших объемах.
  Тип `tsvector` автоматически разбирает текст на нормализованные лексемы (стеммы), удаляет стоп-слова ("the", "is", "at") и строит специальный инвертированный индекс **GIN (Generalized Inverted Index)**.
* **`.Annotation("Npgsql:TsVectorConfig", "english")`**:
  Указывает словарь стемминга. Например, слова *"developing"*, *"developer"*, *"develops"* приводятся к единому корню *"develop"*. Пользователь вбивает в поиск "developer", а система находит вакансии со словами "develop" и "development"!
* **`.Annotation("Npgsql:TsVectorProperties", new[] { "Title", "ShortDescription" })`**:
  Сгенерированная колонка (Generated Column). PostgreSQL автоматически на уровне СУБД обновляет вектор поиска при любом изменении полей `Title` или `ShortDescription`.

---

## 5. Разбор сидирования данных (DevelopmentDataSeeder & DatabaseSeeder)

### 5.1. Зачем IServiceProvider.CreateScope() при старте?

```csharp
public static async Task SeedAsync(IServiceProvider services) {
    using var scope = services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
    ...
}
```

* **Почему нельзя просто вызвать `services.GetRequiredService<AppDbContext>()`:**
  `AppDbContext` и `UserManager` зарегистрированы с временем жизни **Scoped** (они живут в рамках одного HTTP-запроса).
  При старте приложения в методе `Main` HTTP-запроса **нет** (мы находимся в глобальном Root-контейнере).
  Если запросить Scoped-сервис из корневого провайдера, .NET выбросит исключение:  
  *«Cannot resolve scoped service from root provider (Captive Dependency)»*.
  Поэтому мы вручную создаем область видимости `services.CreateScope()`. После выхода из блока `using` контекст базы данных корректно закрывает соединение и освобождает память.

---

### 5.2. Построчный разбор EnsureAttrAsync и предотвращение дубликатов

В файле `DevelopmentDataSeeder.cs`:
```csharp
var existingAttributes = await db.Attributes.Include(a => a.Options).ToDictionaryAsync(a => a.Name);
```
1. **Зачем `.ToDictionaryAsync(a => a.Name)`:**
   Вместо того чтобы в цикле для каждого атрибута слать запрос в базу данных: `SELECT * FROM Attributes WHERE Name = 'CAP'` (что создало бы десятки лишних запросов к БД), мы одним быстрым запросом считываем существующие атрибуты в память в виде хеш-таблицы (словаря). Поиск в словаре `existingAttributes.TryGetValue(name, out var existing)` работает за **O(1)** (мгновенно).
2. **Идемпотентность (Idempotency):**
   Метод проверяет: если атрибут уже есть в базе, он не создает дубликат, а только дописывает недостающие опции:
   ```csharp
   if (existingAttributes.TryGetValue(name, out var existing)) {
       if (options != null && options.Length > 0 && !existing.Options.Any()) {
           for (int i = 0; i < options.Length; i++) {
               existing.Options.Add(new AttributeOption {
                   Id = Guid.NewGuid(),
                   AttributeId = existing.Id,
                   Value = options[i],
                   SortOrder = i
               });
           }
           await db.SaveChangesAsync();
       }
       return existing;
   }
   ```

---

### 5.3. Требования курса: Dropdown "CAP" в категории "Certificates" и другие атрибуты

В коде реализованы конкретные академические требования курсового задания:
```csharp
// 1. Выпадающий список CAP со значениями None, Essentials, Pro, Expert
await EnsureAttrAsync("CAP", "Certificates", AttributeType.Dropdown, 
    "Certified Analytics Professional certification level", 
    ["None", "Essentials", "Pro", "Expert"]);

// 2. Сертификат AWS
await EnsureAttrAsync("AWS Certified Solutions Architect", "Certificates", AttributeType.Dropdown, 
    "AWS cloud architecture certification level", 
    ["None", "Associate", "Professional"]);

// 3. Уровни языка по шкале CEFR
await EnsureAttrAsync("English Level", "Language", AttributeType.Dropdown, 
    "CEFR English proficiency level", 
    ["A1", "A2", "B1", "B2", "C1", "C2"]);

// 4. Числовой балл GPA (4-балльная шкала)
await EnsureAttrAsync("GPA", "Education", AttributeType.Numeric, 
    "Grade Point Average on a 4.0 scale");

// 5. Логические навыки (Boolean)
await EnsureAttrAsync("Python", "Technology", AttributeType.Boolean, 
    "Proficiency in Python programming language");
await EnsureAttrAsync("Apache Hadoop", "Technology", AttributeType.Boolean, 
    "Experience with Apache Hadoop and distributed big data processing");
```

---

## 6. Топ вопросов комиссии с готовыми эталонными ответами

**В1: Как у вас защищены пароли пользователей?**
> **Ответ:** Пароли хешируются с помощью встроенного в ASP.NET Core Identity сервиса `IPasswordHasher<AppUser>`, использующего алгоритм **PBKDF2 с HMAC-SHA256/512** и уникальной криптографической солью (Salt) для каждого пользователя. В базе данных хранится только результат хеширования.

**В2: Почему вы выбрали PostgreSQL, а не MS SQL Server или MongoDB?**
> **Ответ:** PostgreSQL — это мощная, открытая и бесплатная реляционная СУБД. Она идеально обеспечивает ACID-транзакции, поддерживает нативные типы UUID, массивы, JSONB и встроенный полнотекстовый поиск `tsvector`, что позволило нам не подключать сторонний Elasticsearch.

**В3: Что произойдет, если два рекрутера одновременно изменят одну и ту же вакансию?**
> **Ответ:** У нас настроена оптимистическая блокировка через поле `Version` (Concurrency Token). Второй запрос завершится с ошибкой 409 Conflict, и сервер не допустит тихой перезаписи чужих правок.

**В4: Где физически хранятся аватары и файлы резюме?**
> **Ответ:** Они загружаются в объектное S3-совместимое хранилище **Cloudflare R2** с публичным CDN. В базе данных хранятся только уникальные ключи объектов (например, `users/{userId}/{guid}.jpg`), что исключает разрастание базы данных.

**В5: Как защищен API от неавторизованных пользователей?**
> **Ответ:** Защита построена на стандарте JWT (JSON Web Tokens). Контроллеры помечены атрибутом `[Authorize]`. Клиент при каждом запросе передает токен в заголовке `Authorization: Bearer <token>`. Сервер проверяет криптографическую подпись токена с помощью HMAC-SHA256.

---

## 7. Live-Coding на защите: «Открой файл X и добавь фичу Y»

Комиссия очень любит проверять, писал ли студент код сам. Ниже готовые шпаргалки:

### Сценарий 1: «Добавь поле Telegram в профиль пользователя»
1. Открой `backend/backend/Entities/AppUser.cs` и добавь:
   ```csharp
   public string? Telegram { get; set; }
   ```
2. В `backend/backend/DTOs/Profile/ProfileDtos.cs` добавь поле `string? Telegram` в `ProfileView` и `ProfileUpdate`.
3. В `backend/backend/Services/ProfileService.cs` добавь сохранение:
   ```csharp
   user.Telegram = request.Telegram?.Trim();
   ```
4. В терминале создай миграцию:
   ```bash
   dotnet ef migrations add AddTelegramToUser
   ```

### Сценарий 2: «Добавь эндпоинт топ-5 популярных вакансий»
1. В `backend/backend/Services/Interfaces/IPositionService.cs`:
   ```csharp
   Task<List<PositionListItem>> GetTopPopularPositionsAsync(CancellationToken cancellationToken);
   ```
2. В `backend/backend/Services/PositionService.cs`:
   ```csharp
   public async Task<List<PositionListItem>> GetTopPopularPositionsAsync(CancellationToken cancellationToken) {
       return await db.Positions.AsNoTracking()
           .Where(p => p.IsPublic)
           .OrderByDescending(p => p.Cvs.Count)
           .Take(5)
           .Select(p => new PositionListItem(p.Id, p.Title, p.Company, p.Level, p.UpdatedAt, p.IsPublic, p.Cvs.Count))
           .ToListAsync(cancellationToken);
   }
   ```
3. В `backend/backend/Controllers/PositionsController.cs`:
   ```csharp
   [HttpGet("top-popular")]
   [AllowAnonymous]
   public async Task<ActionResult<List<PositionListItem>>> GetTopPopular(CancellationToken cancellationToken) {
       var result = await positionService.GetTopPopularPositionsAsync(cancellationToken);
       return Ok(result);
   }
   ```

---

## 8. Шпаргалка терминов и психологические советы для уверенной защиты

| Термин | Простое объяснение за 5 секунд |
| :--- | :--- |
| **Dependency Injection** | Паттерн, при котором класс не создает свои зависимости через `new`, а получает их извне через конструктор. |
| **REST API** | Архитектура веб-сервиса, использующая стандартные HTTP-методы (GET, POST, PUT, DELETE) и коды ответов (200, 400, 401, 404). |
| **JWT** | Компактный зашифрованный токен, хранящий информацию о пользователе прямо внутри себя (Stateless). |
| **EAV Pattern** | Модель Entity-Attribute-Value, позволяющая динамически добавлять произвольные поля без изменения схемы таблиц БД. |
| **tsvector** | Специальный тип данных PostgreSQL для высокоскоростного полнотекстового поиска с морфологическим анализом слов. |
| **CancellationToken** | Механизм мгновенной отмены асинхронных операций, если клиент разорвал соединение. |
| **Cloudflare R2** | Облачное S3-хранилище файлов с нулевой стоимостью исходящего трафика (Egress-Free). |

### 🎯 3 главных правила на защите:
1. **Говори терминами .NET 10**: Упоминай, что проект построен на самой современной платформе .NET 10, использует Primary Constructors, C# Pattern Matching и чистую слоистую архитектуру (Clean Architecture).
2. **Не теряйся при показе кода**: Структура проекта прозрачна: контроллеры принимают запросы (`Controllers/`), сервисы содержат логику (`Services/`), таблицы описаны в `Entities/`, а настройки базы — в `Data/`.
3. **Отвечай уверенно**: Твой проект развернут в реальном облаке (Render + Cloudflare R2 + PostgreSQL 17) и полностью протестирован.
