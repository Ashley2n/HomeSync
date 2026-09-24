"use client";

import { useAuth } from "@clerk/nextjs";
import { useState } from "react";

const BASE_URL = process.env.NEXT_PUBLIC_BASE_DOTNET_API;
const FETCH_URL = `${BASE_URL}/api/_diag/Whoami/auth`;

const ApiTest = () => {
  const { getToken, isSignedIn } = useAuth();
  const [result, setResult] = useState("");

  async function callWhoami() {
    try {
      const token = await getToken();
      const res = await fetch(FETCH_URL, {
        headers: { Authorization: `Bearer ${token}` },
      });
      const body = await res.json().catch(() => null);
      setResult(
        `${res.status} ${res.statusText}\n${JSON.stringify(body, null, 2)}`,
      );
    } catch (error) {
        setResult(`${error}`)
    }
  }

  if (!isSignedIn) return <p>Please Sign-in First</p>;

  return (
    <div className="w-4/5 h-140 mx-auto bg-slate-800 flex flex-col justify-start items-center p-4 rounded-lg">
      <button
        className="bg-slate-200 rounded-lg px-6 py-2 text-slate-600 "
        onClick={callWhoami}
      >
        Call WhoAmI API
      </button>
      <pre>{result}</pre>
    </div>
  );
};

export default ApiTest;
