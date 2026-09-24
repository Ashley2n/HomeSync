import { afterEach, beforeEach, describe, expect, test, vi } from "vitest";
import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import ApiTest from "@/app/api-test/page";
import { ClerkProvider, useAuth } from "@clerk/nextjs";
import { makeSignedInAuth, makeSignedOutAuth } from "./test-utills";

const mockedUseAuth = vi.mocked(useAuth);
const fetchMock = vi.fn();

vi.mock("@clerk/nextjs", () => ({
  ClerkProvider: ({ children }: { children: React.ReactNode }) => children,
  useAuth: vi.fn(),
}));

const renderComponent = () => {
  return render(
    <ClerkProvider>
      <ApiTest />
    </ClerkProvider>,
  );
};

describe("HomePage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.stubGlobal("fetch", fetchMock);
    mockedUseAuth.mockReturnValue(makeSignedOutAuth());
    fetchMock.mockResolvedValue({
      status: 200,
      statusText: "OK",
      json: () => Promise.resolve({ isAuthenticated: true }),
    });
  });

  afterEach(() => {
    cleanup();
    vi.unstubAllGlobals();
  });

  test("OnPageLoad_GivenUserIsNotLoggedIn_ReturnSignInText", () => {
    renderComponent();
    expect(screen.getByText(/Please Sign-in First/i)).toBeDefined();
  });

  test("OnPageLoad_GivenSignedinUser_ShowWhoamiButton", async () => {
    mockedUseAuth.mockReturnValue(makeSignedInAuth());
    renderComponent();

    expect(
      screen.getByRole("button", { name: /call Whoami api/i }),
    ).toBeDefined();
  });

  test("OnPageLoad_GivenSignedinUserClicksButton_Returns200", async () => {
    mockedUseAuth.mockReturnValue(makeSignedInAuth());
    renderComponent();

    fireEvent.click(screen.getByRole("button", { name: /call Whoami api/i }));
    await screen.findByText(/200/);

    expect(fetchMock).toHaveBeenCalledWith(
      expect.stringContaining("/api/_diag/Whoami/auth"),
      expect.objectContaining({
        headers: { Authorization: "Bearer fake_token" },
      }),
    );
  });

  test("OnPageLoad_GivenSignedinUserClicksButton_ReturnsAResponses", async () => {
    mockedUseAuth.mockReturnValue(makeSignedInAuth());
    renderComponent();

    fireEvent.click(screen.getByRole("button", { name: /call Whoami api/i }));
    await screen.findByText(/200/);

    expect(fetchMock).toHaveResolved();
  });

  test("OnPageLoad_GivenSignedinUserClicksButton_ReturnsValidResponseBody", async () => {
    mockedUseAuth.mockReturnValue(makeSignedInAuth());
    renderComponent();

    fireEvent.click(screen.getByRole("button", { name: /call Whoami api/i }));
    await screen.findByText(/200/);

    expect(fetchMock).toHaveResolvedWith({
      status: 200,
      statusText: "OK",
      json: expect.any(Function),
    });
  });

  test("shows an error state when fetch rejects", async () => {
    fetchMock.mockRejectedValue(new Error("network down"));
    mockedUseAuth.mockReturnValue(makeSignedInAuth());
    renderComponent();

    fireEvent.click(screen.getByRole("button", { name: /call Whoami api/i }));

    expect(await screen.findByText(/network down/i)).toBeDefined();
  });
});
