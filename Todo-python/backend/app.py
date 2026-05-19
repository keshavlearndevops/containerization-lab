import json
import os
from flask import Flask, request, jsonify
from flask_cors import CORS

app = Flask(__name__)
CORS(app)

DATA_FILE = os.environ.get("DATA_FILE", "data/todos.json")


def load():
    if not os.path.exists(DATA_FILE):
        return [], 1
    with open(DATA_FILE) as f:
        saved = json.load(f)
    todos = saved.get("todos", [])
    next_id = saved.get("next_id", 1)
    return todos, next_id


def save(todos, next_id):
    os.makedirs(os.path.dirname(DATA_FILE), exist_ok=True)
    with open(DATA_FILE, "w") as f:
        json.dump({"todos": todos, "next_id": next_id}, f)


@app.route("/todos", methods=["GET"])
def get_todos():
    todos, _ = load()
    return jsonify(todos)


@app.route("/todos", methods=["POST"])
def create_todo():
    todos, next_id = load()
    data = request.get_json()
    if not data or not data.get("title"):
        return jsonify({"error": "title is required"}), 400
    todo = {"id": next_id, "title": data["title"], "done": False}
    todos.append(todo)
    save(todos, next_id + 1)
    return jsonify(todo), 201


@app.route("/todos/<int:todo_id>", methods=["PUT"])
def toggle_todo(todo_id):
    todos, next_id = load()
    for todo in todos:
        if todo["id"] == todo_id:
            todo["done"] = not todo["done"]
            save(todos, next_id)
            return jsonify(todo)
    return jsonify({"error": "not found"}), 404


@app.route("/todos/<int:todo_id>", methods=["DELETE"])
def delete_todo(todo_id):
    todos, next_id = load()
    todos = [t for t in todos if t["id"] != todo_id]
    save(todos, next_id)
    return jsonify({"message": "deleted"})


if __name__ == "__main__":
    app.run(host="0.0.0.0", port=5000, debug=True)
