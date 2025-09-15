import React from "react";

type Todo = { id: string; title: string; done: boolean };

type TodoListProps = {
	items: Todo[];
	onToggle: (id: string) => void;
};

export const TodoList: React.FC<TodoListProps> = ({ items, onToggle }) => (
	<ul>
		{items.map((t) => (
			<li key={t.id}>
				<label>
					<input type="checkbox" checked={t.done} onChange={() => onToggle(t.id)} />
					{t.title}
				</label>
			</li>
		))}
	</ul>
);

export const TodoContainer: React.FC = () => {
	const [todos, setTodos] = React.useState<Todo[]>([
		{ id: "1", title: "Read", done: false },
		{ id: "2", title: "Write", done: true }
	]);

	const handleToggle = (id: string) =>
		setTodos((prev) => prev.map((t) => (t.id === id ? { ...t, done: !t.done } : t)));

	return <TodoList items={todos} onToggle={handleToggle} />;
};