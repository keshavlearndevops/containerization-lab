import os
import requests
from flask import Flask, render_template, request, redirect, url_for

app = Flask(__name__)

# Read backend URL from environment variable (set via ConfigMap in Kubernetes)
BACKEND_URL = os.environ.get("BACKEND_URL", "http://localhost:5000")


@app.route("/")
def index():
    response = requests.get(f"{BACKEND_URL}/todos")
    todos = response.json() if response.ok else []
    return render_template("index.html", todos=todos)


@app.route("/add", methods=["POST"])
def add():
    title = request.form.get("title", "").strip()
    if title:
        requests.post(f"{BACKEND_URL}/todos", json={"title": title})
    return redirect(url_for("index"))


@app.route("/toggle/<int:todo_id>")
def toggle(todo_id):
    requests.put(f"{BACKEND_URL}/todos/{todo_id}")
    return redirect(url_for("index"))


@app.route("/delete/<int:todo_id>")
def delete(todo_id):
    requests.delete(f"{BACKEND_URL}/todos/{todo_id}")
    return redirect(url_for("index"))


if __name__ == "__main__":
    app.run(host="0.0.0.0", port=8080, debug=True)
