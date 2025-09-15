import React from "react";

type CxUser = { id: number; email: string; isActive: boolean };

type ApiClientOptions = { baseUrl?: string };

export function createCxApiClient(options: ApiClientOptions = {}) {
	const baseUrl = options.baseUrl ?? "http://localhost:5000";
	return {
		async getUser(id: number): Promise<CxUser | null> {
			const res = await fetch(`${baseUrl}/users/${id}`);
			if (res.status === 404) return null;
			if (!res.ok) throw new Error(`Get failed: ${res.status}`);
			return res.json();
		},
		async upsertUser(user: CxUser): Promise<void> {
			const res = await fetch(`${baseUrl}/users`, {
				method: "POST",
				headers: { "Content-Type": "application/json" },
				body: JSON.stringify(user)
			});
			if (!res.ok) throw new Error(`Upsert failed: ${res.status}`);
		}
	};
}

export function useUser(id: number, client = createCxApiClient()) {
	const [data, setData] = React.useState<CxUser | null>(null);
	const [loading, setLoading] = React.useState<boolean>(true);
	const [error, setError] = React.useState<string | null>(null);

	React.useEffect(() => {
		let mounted = true;
		setLoading(true);
		client.getUser(id)
			.then((u) => mounted && setData(u))
			.catch((e) => mounted && setError(String(e)))
			.finally(() => mounted && setLoading(false));
		return () => { mounted = false; };
	}, [id]);

	return { data, loading, error };
}

export function CxUserProfile({ id }: { id: number }) {
	const client = React.useMemo(() => createCxApiClient(), []);
	const { data, loading, error } = useUser(id, client);
	if (loading) return <p>Loading...</p>;
	if (error) return <p>Error: {error}</p>;
	if (!data) return <p>Not found</p>;
	return (
		<div>
			<h3>User</h3>
			<p>{data.email} ({data.isActive ? "active" : "inactive"})</p>
		</div>
	);
}