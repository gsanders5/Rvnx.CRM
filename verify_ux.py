from playwright.sync_api import sync_playwright

def run(playwright):
    browser = playwright.chromium.launch(headless=True)
    context = browser.new_context()
    page = context.new_page()

    # 1. Create a Contact
    print("Navigating to Create Contact...")
    page.goto("http://localhost:5215/Contacts/Create")

    print("Filling Contact Form...")
    unique_id = "UX"
    page.fill('input[name="FirstName"]', unique_id)
    page.fill('input[name="LastName"]', "Tester")
    page.click('input[type="submit"]')

    # Wait for navigation to Index page
    page.wait_for_load_state("networkidle")
    print(f"Created Contact. URL: {page.url}")

    # 2. Go to Details
    print("Navigating to Details...")
    if "/Contacts" in page.url and "/Create" not in page.url:
        page.locator(f'a:has-text("{unique_id} Tester")').first.click()

    page.wait_for_url("**/Contacts/Details/**")
    print(f"On Details Page. URL: {page.url}")

    # 3. Add a Fact
    print("Adding a Fact...")

    if page.is_visible("text=No facts added"):
        page.click('a:has-text("Add Fact")')
    else:
        # Check if we already added the fact in previous run (since we are creating new contacts but maybe reused logic)
        # But wait, we create a NEW contact each time. So it should be empty.
        # However, checking both cases is robust.
        if page.is_visible('.card-header:has-text("Quick Facts") a'):
             page.locator('.card-header:has-text("Quick Facts") a').click()
        else:
             print("Could not find Add button")

    page.wait_for_url("**/Facts/Create**")

    print("Filling Fact Form...")
    page.fill('#Category', "General")
    page.fill('#Value', "Copy This Text")
    page.click('input[type="submit"]')

    # Wait for redirect back to Details
    page.wait_for_url("**/Contacts/Details/**")
    print("Fact Added.")

    # 4. Verify and Screenshot
    print("Verifying new buttons...")
    # Wait for the COPY button to be visible
    page.wait_for_selector('.btn-copy[data-clipboard-text="Copy This Text"]')

    facts_header = page.locator('h5:has-text("Quick Facts")')
    facts_card = facts_header.locator("xpath=../..")

    if facts_card.count() > 0:
        print("Found Facts Card. Taking screenshot...")
        facts_card.screenshot(path="verification.png")
    else:
        print("Could not find Facts Card. Screenshotting full page.")
        page.screenshot(path="verification.png", full_page=True)

    browser.close()

with sync_playwright() as playwright:
    run(playwright)
