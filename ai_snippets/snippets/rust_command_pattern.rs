use std::fmt::Debug;

trait Command: Debug {
	fn execute(&self) -> String;
}

#[derive(Debug)]
struct AddItem { item: String }

impl Command for AddItem {
	fn execute(&self) -> String { format!("added:{}", self.item) }
}

#[derive(Debug)]
struct RemoveItem { item: String }

impl Command for RemoveItem {
	fn execute(&self) -> String { format!("removed:{}", self.item) }
}

#[derive(Default)]
struct CommandBus { queue: Vec<Box<dyn Command>> }

impl CommandBus {
	fn enqueue<C: Command + 'static>(&mut self, cmd: C) { self.queue.push(Box::new(cmd)); }
	fn run(&mut self) -> Vec<String> { self.queue.drain(..).map(|c| c.execute()).collect() }
}

fn main() {
	let mut bus = CommandBus::default();
	bus.enqueue(AddItem { item: "apple".into() });
	bus.enqueue(RemoveItem { item: "apple".into() });
	let results = bus.run();
	println!("{:?}", results);
}