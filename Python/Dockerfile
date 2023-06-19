# Используем базовый образ Python
FROM python:3.9

# Установка зависимостей приложения
COPY requirements.txt /app/requirements.txt
RUN pip install --no-cache-dir -r /app/requirements.txt

# Копирование исходного кода приложения в образ
COPY . /app

# Установка рабочей директории
WORKDIR /app

# Запуск команды при запуске контейнера
CMD ["python", "Python.py"]

# Открываем порт, если необходимо
# EXPOSE <порт>

