uLearn
=======

[![Build status](https://tc.skbkontur.ru/app/rest/builds/buildType:\(id:ULearn_Build\)/statusIcon)(https://tc.skbkontur.ru/app/rest/builds/buildType:\(id:ULearn_Build\)/statusIcon)]


E-learning platform and courses



Подмодули
---------

Некоторые курсы хранятся в отдельных репозиториях и подключены в виде подмодулей.

Для получения подмодулей нужно выполнить следующие две команды в директории проекта:
  $ git submodule init
  $ git submodule update
  
После этого можно будет отдельно делать коммиты в проекты с курсами и отдельно, независимо делать коммит в проект uLearn.


Первый запуск
------------

1. Создать файл src\uLearn.Web\Ideone\ExecutionService.Settings.cs на основе 
[ExecutionService.Settings.Sample.cs](src\uLearn.Web\Ideone\ExecutionService.Settings.Sample.cs).
Это файл с данными аккаунта ideone.com. Вы можете создать свой аккаунт. Он нужен, чтобы работала проверка задач.

2. Настроить SSL в IIS

3. При первом запуске будет создана база данных с двумя пользователями: admin (пароль fullcontrol) и user (пароль asdasd).
По умолчанию ни один модуль ни одного курса не опубликован, поэтому при попытке user зайти в курс будет ошибка. 
Первым делом нужно войти как admin, зайти в раздел "Admin courses" и опубликовать один или несколько модулей.

Разработка курсов
-----------------

Каждый курс — это проект. 

Каждый публикуемый модуль — это поддиректория в директории Slides. В директории модуля должен быть файл Title.txt содержащий название модуля.

Каждый слайд — файл в директории модуля, названный с префиксом S01_, S002_ и т.п.

Порядок модулей и слайдов в курсе совпадает с лексикографическим порядком имен соответствующих директорий и файлов.

Post build actions проектов с курсами устроены так, чтобы после успешной сборки все файлы директории Slides запаковывались в zip-файл
и копировались в директорию uLearn.Web\Courses.Staging. 
Веб-приложение при старте читает все курсы из этой директории.

В разделе Admin courses веб приложения можно загрузить новый курс или новую версию курса, а также принудительно перечитать курс из файла в директории Courses.Staging.


