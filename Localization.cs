using System.Globalization;

namespace Experimental56;

public static class Loc
{
    private static string _lang = "ru";
    public static string Language => _lang;
    public static event Action? LanguageChanged;

    private static readonly Dictionary<string, Dictionary<string, string>> D = new()
    {
        ["ru"] = new()
        {
            ["AppTitle"] = "Testing Platform", ["Settings"] = "Настройки", ["Logout"] = "Выйти", ["Users"] = "Пользователи",
            ["Groups"] = "Группы", ["Audit"] = "Аудит", ["Tests"] = "Тесты", ["Assignments"] = "Назначения", ["Results"] = "Результаты",
            ["Constructor"] = "Конструктор", ["AvailableTests"] = "Доступные тесты", ["MyAttempts"] = "Мои попытки", ["Save"] = "Сохранить",
            ["Language"] = "Язык", ["Theme"] = "Тема", ["Database"] = "База данных", ["Backup"] = "Backup", ["Restore"] = "Restore",
            ["ChangePassword"] = "Сменить пароль", ["CurrentPassword"] = "Текущий пароль", ["NewPassword"] = "Новый пароль", ["ConfirmPassword"] = "Подтвердите пароль",
            ["TeacherConstructorEnabled"] = "Конструктор доступен учителям", ["ChooseFolder"] = "Выберите папку", ["ChooseDb"] = "Выберите .db",
            ["Saved"] = "Сохранено", ["Preview"] = "Предпросмотр", ["Exit"] = "Выход", ["NewDraft"] = "Новый Draft", ["CloneToDraft"] = "Клон в Draft",
            ["Publish"] = "Публиковать", ["Archive"] = "Архив", ["AddQuestion"] = "Добавить вопрос", ["Delete"] = "Удалить", ["MoveUp"] = "Вверх", ["MoveDown"] = "Вниз", ["Duplicate"] = "Дублировать",
            ["QuestionText"] = "Текст вопроса", ["Points"] = "Баллы", ["Title"] = "Название", ["Description"] = "Описание", ["PassPercent"] = "Проходной %",
            ["Type"] = "Тип", ["DbPath"] = "Путь к БД"
        },
        ["en"] = new()
        {
            ["AppTitle"] = "Testing Platform", ["Settings"] = "Settings", ["Logout"] = "Logout", ["Users"] = "Users", ["Groups"] = "Groups", ["Audit"] = "Audit",
            ["Tests"] = "Tests", ["Assignments"] = "Assignments", ["Results"] = "Results", ["Constructor"] = "Constructor", ["AvailableTests"] = "Available Tests", ["MyAttempts"] = "My Attempts",
            ["Save"] = "Save", ["Language"] = "Language", ["Theme"] = "Theme", ["Database"] = "Database", ["Backup"] = "Backup", ["Restore"] = "Restore",
            ["ChangePassword"] = "Change password", ["CurrentPassword"] = "Current password", ["NewPassword"] = "New password", ["ConfirmPassword"] = "Confirm password",
            ["TeacherConstructorEnabled"] = "Constructor available for teachers", ["ChooseFolder"] = "Choose folder", ["ChooseDb"] = "Choose .db", ["Saved"] = "Saved",
            ["Preview"] = "Preview", ["Exit"] = "Exit", ["NewDraft"] = "New Draft", ["CloneToDraft"] = "Clone to Draft", ["Publish"] = "Publish", ["Archive"] = "Archive",
            ["AddQuestion"] = "Add question", ["Delete"] = "Delete", ["MoveUp"] = "Move Up", ["MoveDown"] = "Move Down", ["Duplicate"] = "Duplicate",
            ["QuestionText"] = "Question text", ["Points"] = "Points", ["Title"] = "Title", ["Description"] = "Description", ["PassPercent"] = "Pass %", ["Type"] = "Type", ["DbPath"] = "DB path"
        },
        ["kz"] = new()
        {
            ["AppTitle"] = "Тестілеу платформасы", ["Settings"] = "Баптаулар", ["Logout"] = "Шығу", ["Users"] = "Пайдаланушылар", ["Groups"] = "Топтар", ["Audit"] = "Аудит",
            ["Tests"] = "Тесттер", ["Assignments"] = "Тағайындаулар", ["Results"] = "Нәтижелер", ["Constructor"] = "Конструктор", ["AvailableTests"] = "Қолжетімді тесттер", ["MyAttempts"] = "Әрекеттерім",
            ["Save"] = "Сақтау", ["Language"] = "Тіл", ["Theme"] = "Тақырып", ["Database"] = "Дерекқор", ["Backup"] = "Көшірме", ["Restore"] = "Қалпына келтіру",
            ["ChangePassword"] = "Құпиясөзді өзгерту", ["CurrentPassword"] = "Ағымдағы құпиясөз", ["NewPassword"] = "Жаңа құпиясөз", ["ConfirmPassword"] = "Құпиясөзді растау",
            ["TeacherConstructorEnabled"] = "Конструктор мұғалімге қолжетімді", ["ChooseFolder"] = "Қапшықты таңдаңыз", ["ChooseDb"] = ".db таңдаңыз", ["Saved"] = "Сақталды",
            ["Preview"] = "Алдын ала көру", ["Exit"] = "Шығу", ["NewDraft"] = "Жаңа Draft", ["CloneToDraft"] = "Draft-қа клон", ["Publish"] = "Жариялау", ["Archive"] = "Мұрағат",
            ["AddQuestion"] = "Сұрақ қосу", ["Delete"] = "Жою", ["MoveUp"] = "Жоғары", ["MoveDown"] = "Төмен", ["Duplicate"] = "Көшіру",
            ["QuestionText"] = "Сұрақ мәтіні", ["Points"] = "Ұпай", ["Title"] = "Атауы", ["Description"] = "Сипаттама", ["PassPercent"] = "Өту %", ["Type"] = "Түрі", ["DbPath"] = "ДҚ жолы"
        }
    };

    public static string T(string key) => D.TryGetValue(_lang, out var lang) && lang.TryGetValue(key, out var val) ? val : key;
    public static void SetLanguage(string lang)
    {
        _lang = D.ContainsKey(lang) ? lang : "ru";
        CultureInfo.CurrentUICulture = new CultureInfo(_lang == "kz" ? "kk-KZ" : _lang == "en" ? "en-US" : "ru-RU");
        LanguageChanged?.Invoke();
    }
}
