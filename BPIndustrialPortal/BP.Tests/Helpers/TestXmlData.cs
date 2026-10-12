namespace BP.Tests.Helpers;

/// <summary>
/// Minimal XML strings used by unit tests — isolated from the real Data files.
/// </summary>
internal static class TestXmlData
{
    public static string HomeMetrics { get; } = """
        <?xml version="1.0" encoding="UTF-8"?>
        <BrokerHomeMetrics>
            <LastUpdated>2026-06-01T10:00:00</LastUpdated>
            <SummaryMetrics>
                <CustomersManaged>
                    <Total>50</Total>
                    <NewThisMonth>3</NewThisMonth>
                    <Churned>1</Churned>
                </CustomersManaged>
                <ActiveContracts>
                    <Total>100</Total>
                    <Fixed>60</Fixed>
                    <Variable>40</Variable>
                </ActiveContracts>
                <UpcomingRenewals>
                    <Total>10</Total>
                    <Pending>2</Pending>
                    <AvgDaysToExpiry>20</AvgDaysToExpiry>
                </UpcomingRenewals>
                <MetersManaged>
                    <Total>200</Total>
                    <Decreased>120</Decreased>
                    <Increased>80</Increased>
                </MetersManaged>
                <AnnualUsage>
                    <TotalMWh>45000</TotalMWh>
                    <YoYChange>+3.0%</YoYChange>
                </AnnualUsage>
            </SummaryMetrics>
            <RecentActivity>
                <ContractAmendmentsSigned>
                    <Count>5</Count>
                    <Label>Across all customers</Label>
                </ContractAmendmentsSigned>
                <OpenSupportRequests>
                    <Count>8</Count>
                    <HighPriority>3</HighPriority>
                </OpenSupportRequests>
                <UpcomingContractRenewals>
                    <Count>10</Count>
                    <DueWithinDays>90</DueWithinDays>
                </UpcomingContractRenewals>
                <EstimatedRevenueAtRenewal>
                    <Amount>150000</Amount>
                    <RenewalCount>10</RenewalCount>
                </EstimatedRevenueAtRenewal>
            </RecentActivity>
        </BrokerHomeMetrics>
        """;

    /// <summary>
    /// 5 entries covering all filter buckets:
    ///   0-15  → Epsilon LLC (5), Alpha Corp (10)   = 2
    ///   16-30 → Beta Inc (20)                      = 1
    ///   31-60 → Delta Ltd (45)                     = 1
    ///   61-90 → Gamma Co (75)                      = 1
    /// </summary>
    public static string RenewalWatchlist { get; } = """
        <?xml version="1.0" encoding="UTF-8"?>
        <BrokerRenewalWatchlist>
            <LastUpdated>2026-06-01T10:00:00</LastUpdated>
            <Items>
                <Item>
                    <Customer>Alpha Corp</Customer>
                    <BrokerMargin>0.050</BrokerMargin>
                    <TermMonths>12</TermMonths>
                    <VolumeMWh>5000</VolumeMWh>
                    <DaysRemaining>10</DaysRemaining>
                </Item>
                <Item>
                    <Customer>Beta Inc</Customer>
                    <BrokerMargin>0.025</BrokerMargin>
                    <TermMonths>24</TermMonths>
                    <VolumeMWh>8000</VolumeMWh>
                    <DaysRemaining>20</DaysRemaining>
                </Item>
                <Item>
                    <Customer>Delta Ltd</Customer>
                    <BrokerMargin>0.100</BrokerMargin>
                    <TermMonths>36</TermMonths>
                    <VolumeMWh>3000</VolumeMWh>
                    <DaysRemaining>45</DaysRemaining>
                </Item>
                <Item>
                    <Customer>Gamma Co</Customer>
                    <BrokerMargin>0.030</BrokerMargin>
                    <TermMonths>18</TermMonths>
                    <VolumeMWh>12000</VolumeMWh>
                    <DaysRemaining>75</DaysRemaining>
                </Item>
                <Item>
                    <Customer>Epsilon LLC</Customer>
                    <BrokerMargin>0.075</BrokerMargin>
                    <TermMonths>12</TermMonths>
                    <VolumeMWh>6000</VolumeMWh>
                    <DaysRemaining>5</DaysRemaining>
                </Item>
            </Items>
        </BrokerRenewalWatchlist>
        """;

    /// <summary>
    /// 4 entries:
    ///   Acme Inc   – Billing Dispute    – HIGH   – 5/10/2026
    ///   Beta Corp  – Information Request– LOW    – 5/5/2026
    ///   Alpha Ltd  – Other              – MEDIUM – 5/8/2026
    ///   Delta Co   – Billing Dispute    – HIGH   – 5/12/2026
    /// </summary>
    public static string OpenSupportRequests { get; } = """
        <?xml version="1.0" encoding="UTF-8"?>
        <BrokerOpenSupportRequests>
            <LastUpdated>2026-06-01T10:00:00</LastUpdated>
            <Items>
                <Item>
                    <Customer>Acme Inc</Customer>
                    <RequestType>Billing Dispute</RequestType>
                    <Priority>HIGH</Priority>
                    <CreatedOn>5/10/2026</CreatedOn>
                </Item>
                <Item>
                    <Customer>Beta Corp</Customer>
                    <RequestType>Information Request</RequestType>
                    <Priority>LOW</Priority>
                    <CreatedOn>5/5/2026</CreatedOn>
                </Item>
                <Item>
                    <Customer>Alpha Ltd</Customer>
                    <RequestType>Other</RequestType>
                    <Priority>MEDIUM</Priority>
                    <CreatedOn>5/8/2026</CreatedOn>
                </Item>
                <Item>
                    <Customer>Delta Co</Customer>
                    <RequestType>Billing Dispute</RequestType>
                    <Priority>HIGH</Priority>
                    <CreatedOn>5/12/2026</CreatedOn>
                </Item>
            </Items>
        </BrokerOpenSupportRequests>
        """;

    /// <summary>
    /// 2 customers, 5 contracts total, 7 quotes total.
    ///
    /// Alpha Customer (3 contracts):
    ///   TEST-C001 – 100 Main Street – Days=10 – QUOTE SENT  (expiring, open)
    ///   TEST-C002 – 100 Main Street – Days=20 – REQUESTED   (expiring, pending)
    ///   TEST-C003 – 200 Oak Avenue  – Days=45 – APPROVED    (not expiring)
    ///   → TotalContracts=3, ExpiringContracts(≤30)=2, PendingRequests=2
    ///   → Cards: Active=3, RenewalPending=1, OpenRequest=1, QuotesApproved=1
    ///
    /// Beta Customer (2 contracts):
    ///   TEST-C004 – 300 Pine Road   – Days=5  – REQUESTED   (expiring, pending)
    ///   TEST-C005 – 400 Elm Street  – Days=60 – APPROVED    (not expiring)
    ///   → TotalContracts=2, ExpiringContracts(≤30)=1, PendingRequests=1
    ///   → Cards: Active=2, RenewalPending=1, OpenRequest=0, QuotesApproved=1
    /// </summary>
    public static string BrokerRenewals { get; } = """
        <?xml version="1.0" encoding="UTF-8"?>
        <BrokerRenewals>
            <Summary>
                <UpcomingRenewals>20</UpcomingRenewals>
                <CustomersImpacted>5</CustomersImpacted>
                <RenewalDueThisWeek>4</RenewalDueThisWeek>
                <DueToday>1</DueToday>
                <PendingRenewalRequests>3</PendingRenewalRequests>
                <NewRequestsToday>2</NewRequestsToday>
                <QuotesPending>6</QuotesPending>
                <CustomersAwaitingQuotes>3</CustomersAwaitingQuotes>
                <TotalEntries>10</TotalEntries>
            </Summary>
            <Customers>
                <Customer>
                    <CompanyName>Alpha Customer</CompanyName>
                    <Contracts>
                        <Contract>
                            <ContractId>TEST-C001</ContractId>
                            <ServiceAddress>100 Main Street</ServiceAddress>
                            <ContractStatus>ACTIVE</ContractStatus>
                            <CurrentPlan>24</CurrentPlan>
                            <StartDate>1/1/2024</StartDate>
                            <EndDate>7/20/2026</EndDate>
                            <DaysRemaining>10</DaysRemaining>
                            <LastQuoteStatus>QUOTE SENT</LastQuoteStatus>
                            <ContractTerm>24</ContractTerm>
                            <ProductType>Fixed</ProductType>
                            <MeterCount>3</MeterCount>
                            <QuoteHistory>
                                <Quote>
                                    <QuoteId>QT-TA001</QuoteId>
                                    <RequestedOn>5/15/2026</RequestedOn>
                                    <RequestedPlan>Market Index</RequestedPlan>
                                    <TermLength>24</TermLength>
                                    <QuotedPrice>62.00</QuotedPrice>
                                    <BrokerMargin>0.025</BrokerMargin>
                                    <Status>QUOTE SENT</Status>
                                    <SentOn>5/25/2026</SentOn>
                                </Quote>
                                <Quote>
                                    <QuoteId>QT-TA002</QuoteId>
                                    <RequestedOn>5/1/2026</RequestedOn>
                                    <RequestedPlan>Fixed Price</RequestedPlan>
                                    <TermLength>18</TermLength>
                                    <QuotedPrice>63.50</QuotedPrice>
                                    <BrokerMargin>0.030</BrokerMargin>
                                    <Status>REJECTED</Status>
                                    <SentOn>5/5/2026</SentOn>
                                </Quote>
                            </QuoteHistory>
                        </Contract>
                        <Contract>
                            <ContractId>TEST-C002</ContractId>
                            <ServiceAddress>100 Main Street</ServiceAddress>
                            <ContractStatus>ACTIVE</ContractStatus>
                            <CurrentPlan>36</CurrentPlan>
                            <EndDate>7/30/2026</EndDate>
                            <DaysRemaining>20</DaysRemaining>
                            <LastQuoteStatus>REQUESTED</LastQuoteStatus>
                            <QuoteHistory>
                                <Quote>
                                    <QuoteId>QT-TA003</QuoteId>
                                    <RequestedOn>5/10/2026</RequestedOn>
                                    <RequestedPlan>Fixed Price</RequestedPlan>
                                    <TermLength>24</TermLength>
                                    <QuotedPrice>61.00</QuotedPrice>
                                    <BrokerMargin>0.040</BrokerMargin>
                                    <Status>REQUESTED</Status>
                                    <SentOn></SentOn>
                                </Quote>
                                <Quote>
                                    <QuoteId>QT-TA004</QuoteId>
                                    <RequestedOn>4/20/2026</RequestedOn>
                                    <RequestedPlan>Market Index</RequestedPlan>
                                    <TermLength>24</TermLength>
                                    <QuotedPrice>62.80</QuotedPrice>
                                    <BrokerMargin>0.035</BrokerMargin>
                                    <Status>REJECTED</Status>
                                    <SentOn>4/25/2026</SentOn>
                                </Quote>
                            </QuoteHistory>
                        </Contract>
                        <Contract>
                            <ContractId>TEST-C003</ContractId>
                            <ServiceAddress>200 Oak Avenue</ServiceAddress>
                            <ContractStatus>ACTIVE</ContractStatus>
                            <CurrentPlan>24</CurrentPlan>
                            <EndDate>8/24/2026</EndDate>
                            <DaysRemaining>45</DaysRemaining>
                            <LastQuoteStatus>APPROVED</LastQuoteStatus>
                            <QuoteHistory>
                                <Quote>
                                    <QuoteId>QT-TA005</QuoteId>
                                    <RequestedOn>5/8/2026</RequestedOn>
                                    <RequestedPlan>Fixed Price</RequestedPlan>
                                    <TermLength>24</TermLength>
                                    <QuotedPrice>60.50</QuotedPrice>
                                    <BrokerMargin>0.020</BrokerMargin>
                                    <Status>APPROVED</Status>
                                    <SentOn>5/20/2026</SentOn>
                                </Quote>
                            </QuoteHistory>
                        </Contract>
                    </Contracts>
                </Customer>
                <Customer>
                    <CompanyName>Beta Customer</CompanyName>
                    <Contracts>
                        <Contract>
                            <ContractId>TEST-C004</ContractId>
                            <ServiceAddress>300 Pine Road</ServiceAddress>
                            <ContractStatus>ACTIVE</ContractStatus>
                            <CurrentPlan>24</CurrentPlan>
                            <EndDate>7/15/2026</EndDate>
                            <DaysRemaining>5</DaysRemaining>
                            <LastQuoteStatus>REQUESTED</LastQuoteStatus>
                            <QuoteHistory>
                                <Quote>
                                    <QuoteId>QT-TB001</QuoteId>
                                    <RequestedOn>5/12/2026</RequestedOn>
                                    <RequestedPlan>Fixed Price</RequestedPlan>
                                    <TermLength>24</TermLength>
                                    <QuotedPrice>61.50</QuotedPrice>
                                    <BrokerMargin>0.050</BrokerMargin>
                                    <Status>REQUESTED</Status>
                                    <SentOn></SentOn>
                                </Quote>
                            </QuoteHistory>
                        </Contract>
                        <Contract>
                            <ContractId>TEST-C005</ContractId>
                            <ServiceAddress>400 Elm Street</ServiceAddress>
                            <ContractStatus>ACTIVE</ContractStatus>
                            <CurrentPlan>24</CurrentPlan>
                            <EndDate>9/8/2026</EndDate>
                            <DaysRemaining>60</DaysRemaining>
                            <LastQuoteStatus>APPROVED</LastQuoteStatus>
                            <QuoteHistory>
                                <Quote>
                                    <QuoteId>QT-TB002</QuoteId>
                                    <RequestedOn>4/25/2026</RequestedOn>
                                    <RequestedPlan>Market Index</RequestedPlan>
                                    <TermLength>24</TermLength>
                                    <QuotedPrice>62.30</QuotedPrice>
                                    <BrokerMargin>0.025</BrokerMargin>
                                    <Status>APPROVED</Status>
                                    <SentOn>5/5/2026</SentOn>
                                </Quote>
                            </QuoteHistory>
                        </Contract>
                    </Contracts>
                </Customer>
            </Customers>
        </BrokerRenewals>
        """;

    /// <summary>
    /// One contract (TEST-P001) with two snapshots and two term columns.
    /// All "BestValue" cells use the correct post-fix range name.
    /// </summary>
    public static string PricingIntelligence { get; } = """
        <?xml version="1.0" encoding="utf-8"?>
        <PricingIntelligenceData>
            <ContractPricing priceUnit="$/MWh" contractId="TEST-P001">
                <Snapshot id="today" label="Today">
                    <TermColumns>
                        <Term months="12" recommended="false" />
                        <Term months="36" recommended="true"  />
                    </TermColumns>
                    <Rows>
                        <Row label="On-Peak">
                            <Price term="12" value="63.75" range="High"      />
                            <Price term="36" value="57.90" range="VeryLow"   />
                        </Row>
                        <Row label="Off-Peak">
                            <Price term="12" value="50.60" range="High"      />
                            <Price term="36" value="45.10" range="BestValue" />
                        </Row>
                        <Row label="All Hours">
                            <Price term="12" value="56.80" range="High"      />
                            <Price term="36" value="51.20" range="BestValue" />
                        </Row>
                    </Rows>
                </Snapshot>
                <Snapshot id="1week" label="1 Week Ago">
                    <TermColumns>
                        <Term months="12" recommended="false" />
                        <Term months="36" recommended="true"  />
                    </TermColumns>
                    <Rows>
                        <Row label="On-Peak">
                            <Price term="12" value="65.20" range="High"      />
                            <Price term="36" value="59.30" range="VeryLow"   />
                        </Row>
                        <Row label="Off-Peak">
                            <Price term="12" value="51.80" range="High"      />
                            <Price term="36" value="46.40" range="BestValue" />
                        </Row>
                        <Row label="All Hours">
                            <Price term="12" value="58.30" range="High"      />
                            <Price term="36" value="52.85" range="BestValue" />
                        </Row>
                    </Rows>
                </Snapshot>
            </ContractPricing>
        </PricingIntelligenceData>
        """;

    /// <summary>
    /// One contract (TEST-P001) with two term series (12 and 36 month), three data points each.
    /// The middle point is flagged isToday="true".
    /// </summary>
    public static string PricingTrend { get; } = """
        <?xml version="1.0" encoding="utf-8"?>
        <PricingTrendData>
            <ContractTrend contractId="TEST-P001" loadType="All Hours" priceUnit="$/MWh">
                <Series termMonths="12">
                    <Point date="2026-04-06" value="67.00" isToday="false" />
                    <Point date="2026-05-04" value="59.00" isToday="true"  />
                    <Point date="2026-06-01" value="60.00" isToday="false" />
                </Series>
                <Series termMonths="36">
                    <Point date="2026-04-06" value="58.50" isToday="false" />
                    <Point date="2026-05-04" value="50.00" isToday="true"  />
                    <Point date="2026-06-01" value="54.00" isToday="false" />
                </Series>
            </ContractTrend>
        </PricingTrendData>
        """;

    /// <summary>
    /// One contract (TEST-P001) snapshot detail record matching the today pricing data.
    /// </summary>
    public static string PricingSnapshotDetails { get; } = """
        <?xml version="1.0" encoding="utf-8"?>
        <PricingSnapshotDetailsData>
            <ContractSnapshotDetails contractId="TEST-P001">
                <Date>2026-05-06</Date>
                <PricingZone>ERCOT North</PricingZone>
                <Product>Standard Fixed</Product>
                <TodaysLowestPrice>
                    <TermMonths>36</TermMonths>
                    <Price>45.10</Price>
                    <PriceUnit>$/MWh</PriceUnit>
                    <ChangeVsYesterday>-1.20</ChangeVsYesterday>
                    <ChangePercentVsYesterday>-2.6</ChangePercentVsYesterday>
                </TodaysLowestPrice>
            </ContractSnapshotDetails>
        </PricingSnapshotDetailsData>
        """;

    /// <summary>
    /// 3 statements:
    ///   "S1" – PENDING  – 2 Forecasted transactions (T001, T002)
    ///   "S2" – APPROVED – 1 Current transaction (T010)
    ///   "S3" – REJECTED – 1 Forecasted transaction (T020)
    /// Agency: AgencyId=10, LegalName=Test Agency, StatementEmail=test@example.com
    /// </summary>
    public static string CommissionStatements { get; } = """
        <?xml version="1.0" encoding="utf-8"?>
        <StatementsData>
          <Statements>
            <Statement>
              <StatementId>S1</StatementId>
              <AgencyId>10</AgencyId>
              <LegalName>Test Agency</LegalName>
              <Contact>Test Agency</Contact>
              <Email>NA</Email>
              <PhoneNumber>NA</PhoneNumber>
              <StatementEmail>test@example.com</StatementEmail>
              <StatementDate>01/15/2026</StatementDate>
              <Payments>100.00</Payments>
              <Adjustments>0.00</Adjustments>
              <Clawbacks>-10.00</Clawbacks>
              <SubTotal>100.00</SubTotal>
              <Total>90.00</Total>
              <TotalAmountOwed>90.00</TotalAmountOwed>
              <TotalWriteOffAmount>0.00</TotalWriteOffAmount>
              <TotalAdjustments>0.00</TotalAdjustments>
              <Status>PENDING</Status>
              <PaymentDate>NA</PaymentDate>
              <Comments></Comments>
              <Transactions>
                <Transaction>
                  <TransactionId>T001</TransactionId>
                  <CustomerName>Alpha Corp</CustomerName>
                  <UtilityNumber>111111111111</UtilityNumber>
                  <ContractNumber>C001</ContractNumber>
                  <BillDate>01/01/2026</BillDate>
                  <StartDate>01/01/2026</StartDate>
                  <EndDate>06/30/2026</EndDate>
                  <Type>Payment</Type>
                  <Usage>500.00</Usage>
                  <ChannelFee>0.00200</ChannelFee>
                  <BrokerFee>0.00300</BrokerFee>
                  <UnitFee>0.00500</UnitFee>
                  <Commission>2.50</Commission>
                  <CommodityType>Electric</CommodityType>
                  <TransactionState>Forecasted</TransactionState>
                </Transaction>
                <Transaction>
                  <TransactionId>T002</TransactionId>
                  <CustomerName>Beta Inc</CustomerName>
                  <UtilityNumber>222222222222</UtilityNumber>
                  <ContractNumber>C002</ContractNumber>
                  <BillDate>01/15/2026</BillDate>
                  <StartDate>01/15/2026</StartDate>
                  <EndDate>07/14/2026</EndDate>
                  <Type>Payment</Type>
                  <Usage>1000.00</Usage>
                  <ChannelFee>0.00100</ChannelFee>
                  <BrokerFee>0.00400</BrokerFee>
                  <UnitFee>0.00500</UnitFee>
                  <Commission>5.00</Commission>
                  <CommodityType>Gas</CommodityType>
                  <TransactionState>Forecasted</TransactionState>
                </Transaction>
              </Transactions>
            </Statement>
            <Statement>
              <StatementId>S2</StatementId>
              <AgencyId>10</AgencyId>
              <LegalName>Test Agency</LegalName>
              <Contact>Test Agency</Contact>
              <Email>NA</Email>
              <PhoneNumber>NA</PhoneNumber>
              <StatementEmail>test@example.com</StatementEmail>
              <StatementDate>12/15/2025</StatementDate>
              <Payments>200.00</Payments>
              <Adjustments>0.00</Adjustments>
              <Clawbacks>0.00</Clawbacks>
              <SubTotal>200.00</SubTotal>
              <Total>200.00</Total>
              <TotalAmountOwed>200.00</TotalAmountOwed>
              <TotalWriteOffAmount>0.00</TotalWriteOffAmount>
              <TotalAdjustments>0.00</TotalAdjustments>
              <Status>APPROVED</Status>
              <PaymentDate>01/01/2026</PaymentDate>
              <Comments>Approved on time.</Comments>
              <Transactions>
                <Transaction>
                  <TransactionId>T010</TransactionId>
                  <CustomerName>Gamma Ltd</CustomerName>
                  <UtilityNumber>333333333333</UtilityNumber>
                  <ContractNumber>C010</ContractNumber>
                  <BillDate>12/01/2025</BillDate>
                  <StartDate>12/01/2025</StartDate>
                  <EndDate>05/31/2026</EndDate>
                  <Type>Payment</Type>
                  <Usage>2000.00</Usage>
                  <ChannelFee>0.00000</ChannelFee>
                  <BrokerFee>0.00500</BrokerFee>
                  <UnitFee>0.00500</UnitFee>
                  <Commission>10.00</Commission>
                  <CommodityType>Electric</CommodityType>
                  <TransactionState>Current</TransactionState>
                </Transaction>
              </Transactions>
            </Statement>
            <Statement>
              <StatementId>S3</StatementId>
              <AgencyId>10</AgencyId>
              <LegalName>Test Agency</LegalName>
              <Contact>Test Agency</Contact>
              <Email>NA</Email>
              <PhoneNumber>NA</PhoneNumber>
              <StatementEmail>NA</StatementEmail>
              <StatementDate>11/15/2025</StatementDate>
              <Payments>50.00</Payments>
              <Adjustments>0.00</Adjustments>
              <Clawbacks>0.00</Clawbacks>
              <SubTotal>50.00</SubTotal>
              <Total>50.00</Total>
              <TotalAmountOwed>50.00</TotalAmountOwed>
              <TotalWriteOffAmount>0.00</TotalWriteOffAmount>
              <TotalAdjustments>0.00</TotalAdjustments>
              <Status>REJECTED</Status>
              <PaymentDate>NA</PaymentDate>
              <Comments>Insufficient supporting docs.</Comments>
              <Transactions>
                <Transaction>
                  <TransactionId>T020</TransactionId>
                  <CustomerName>Delta Co</CustomerName>
                  <UtilityNumber>444444444444</UtilityNumber>
                  <ContractNumber>C020</ContractNumber>
                  <BillDate>11/01/2025</BillDate>
                  <StartDate>11/01/2025</StartDate>
                  <EndDate>04/30/2026</EndDate>
                  <Type>Payment</Type>
                  <Usage>750.00</Usage>
                  <ChannelFee>0.00200</ChannelFee>
                  <BrokerFee>0.00300</BrokerFee>
                  <UnitFee>0.00500</UnitFee>
                  <Commission>3.75</Commission>
                  <CommodityType>Gas</CommodityType>
                  <TransactionState>Forecasted</TransactionState>
                </Transaction>
              </Transactions>
            </Statement>
          </Statements>
        </StatementsData>
        """;

    /// <summary>
    /// 3 deals for export/list tests.
    ///   Alpha Corp  → AgencyId A1
    ///   Beta Inc    → AgencyId A2
    ///   Gamma Ltd   → AgencyId A1
    /// </summary>
    public static string CommissionDeals { get; } = """
        <?xml version="1.0" encoding="utf-8"?>
        <DealsData>
          <Deals>
            <Deal>
              <AgencyId>A1</AgencyId>
              <CompanyName>Alpha Corp</CompanyName>
              <ContractNumber>C-ALPHA-001</ContractNumber>
              <ContractStartDate>01/01/2026</ContractStartDate>
              <Term>12</Term>
              <CommissionPlan>6 Months Upfront</CommissionPlan>
              <TermVolume>500.00</TermVolume>
              <UnitFee>0.00500</UnitFee>
              <CurrentPaymentDate>NA</CurrentPaymentDate>
              <CurrentPayment>0.00</CurrentPayment>
            </Deal>
            <Deal>
              <AgencyId>A2</AgencyId>
              <CompanyName>Beta Inc</CompanyName>
              <ContractNumber>C-BETA-002</ContractNumber>
              <ContractStartDate>03/01/2026</ContractStartDate>
              <Term>24</Term>
              <CommissionPlan>12 Months Upfront</CommissionPlan>
              <TermVolume>1000.00</TermVolume>
              <UnitFee>0.00900</UnitFee>
              <CurrentPaymentDate>NA</CurrentPaymentDate>
              <CurrentPayment>0.00</CurrentPayment>
            </Deal>
            <Deal>
              <AgencyId>A1</AgencyId>
              <CompanyName>Gamma Ltd</CompanyName>
              <ContractNumber>C-GAMMA-003</ContractNumber>
              <ContractStartDate>06/01/2025</ContractStartDate>
              <Term>12</Term>
              <CommissionPlan>Monthly</CommissionPlan>
              <TermVolume>2000.00</TermVolume>
              <UnitFee>0.00500</UnitFee>
              <CurrentPaymentDate>07/01/2025</CurrentPaymentDate>
              <CurrentPayment>10.00</CurrentPayment>
            </Deal>
          </Deals>
        </DealsData>
        """;

    /// <summary>
    /// 3 agencies for search/dashboard tests.
    ///   Agency "A1" – "Apex Energy"     – ContactName="apex contact" – Contracts=10
    ///   Agency "A2" – "Beta Brokers"    – ContactName="beta contact" – Contracts=5
    ///   Agency "A3" – "Apex Solutions"  – ContactName="shared name"  – Contracts=8
    /// OnFlowDeals: 2 data points each
    /// PaymentHistory: 2 data points (1 forecasted each)
    /// Agents: 2 per agency
    /// </summary>
    public static string CommissionAgencies { get; } = """
        <?xml version="1.0" encoding="utf-8"?>
        <AgenciesData>
          <Agencies>
            <Agency>
              <AgencyId>A1</AgencyId>
              <AgencyName>Apex Energy</AgencyName>
              <ContactName>apex contact</ContactName>
              <NumberOfContracts>10</NumberOfContracts>
              <Address>
                <Street>100 Main St</Street>
                <City>Austin</City>
                <State>TX</State>
                <Zip>78701</Zip>
              </Address>
              <NumberOfAgents>3</NumberOfAgents>
              <NumberOfDeals>10</NumberOfDeals>
              <CurrentPayment>500.00</CurrentPayment>
              <CurrentPaymentPeriod>Jun-26</CurrentPaymentPeriod>
              <PreviousPayment>400.00</PreviousPayment>
              <PreviousPaymentPeriod>May-26</PreviousPaymentPeriod>
              <ForecastedPayment>600.00</ForecastedPayment>
              <YearToDatePayment>900.00</YearToDatePayment>
              <OnFlowDeals>
                <DataPoint><Month>May-26</Month><DealCount>4</DealCount></DataPoint>
                <DataPoint><Month>Jun-26</Month><DealCount>6</DealCount></DataPoint>
              </OnFlowDeals>
              <PaymentHistory>
                <DataPoint><Month>May-26</Month><Amount>400.00</Amount><IsForecasted>false</IsForecasted></DataPoint>
                <DataPoint><Month>Jul-26</Month><Amount>600.00</Amount><IsForecasted>true</IsForecasted></DataPoint>
              </PaymentHistory>
              <Agents>
                <Agent>
                  <AgentId>AG01</AgentId>
                  <AgentName>Alice</AgentName>
                  <Role>Senior Agent</Role>
                  <Email>alice@apexenergy.example.com</Email>
                  <Phone>555-001-0001</Phone>
                  <ActiveDeals>3</ActiveDeals>
                </Agent>
                <Agent>
                  <AgentId>AG02</AgentId>
                  <AgentName>Bob</AgentName>
                  <Role>Sales Agent</Role>
                  <Email>bob@apexenergy.example.com</Email>
                  <Phone>555-001-0002</Phone>
                  <ActiveDeals>2</ActiveDeals>
                </Agent>
              </Agents>
            </Agency>
            <Agency>
              <AgencyId>A2</AgencyId>
              <AgencyName>Beta Brokers</AgencyName>
              <ContactName>beta contact</ContactName>
              <NumberOfContracts>5</NumberOfContracts>
              <Address>
                <Street>200 Oak Ave</Street>
                <City>Denver</City>
                <State>CO</State>
                <Zip>80201</Zip>
              </Address>
              <NumberOfAgents>2</NumberOfAgents>
              <NumberOfDeals>5</NumberOfDeals>
              <CurrentPayment>150.00</CurrentPayment>
              <CurrentPaymentPeriod>Jun-26</CurrentPaymentPeriod>
              <PreviousPayment>100.00</PreviousPayment>
              <PreviousPaymentPeriod>May-26</PreviousPaymentPeriod>
              <ForecastedPayment>200.00</ForecastedPayment>
              <YearToDatePayment>250.00</YearToDatePayment>
              <OnFlowDeals>
                <DataPoint><Month>May-26</Month><DealCount>2</DealCount></DataPoint>
                <DataPoint><Month>Jun-26</Month><DealCount>3</DealCount></DataPoint>
              </OnFlowDeals>
              <PaymentHistory>
                <DataPoint><Month>May-26</Month><Amount>100.00</Amount><IsForecasted>false</IsForecasted></DataPoint>
                <DataPoint><Month>Aug-26</Month><Amount>200.00</Amount><IsForecasted>true</IsForecasted></DataPoint>
              </PaymentHistory>
              <Agents>
                <Agent>
                  <AgentId>AG10</AgentId>
                  <AgentName>Carol</AgentName>
                  <Role>Account Manager</Role>
                  <Email>carol@betabrokers.example.com</Email>
                  <Phone>555-002-0010</Phone>
                  <ActiveDeals>2</ActiveDeals>
                </Agent>
                <Agent>
                  <AgentId>AG11</AgentId>
                  <AgentName>Dave</AgentName>
                  <Role>Sales Agent</Role>
                  <Email>dave@betabrokers.example.com</Email>
                  <Phone>555-002-0011</Phone>
                  <ActiveDeals>1</ActiveDeals>
                </Agent>
              </Agents>
            </Agency>
            <Agency>
              <AgencyId>A3</AgencyId>
              <AgencyName>Apex Solutions</AgencyName>
              <ContactName>shared name</ContactName>
              <NumberOfContracts>8</NumberOfContracts>
              <Address>
                <Street>300 Pine Rd</Street>
                <City>Seattle</City>
                <State>WA</State>
                <Zip>98101</Zip>
              </Address>
              <NumberOfAgents>4</NumberOfAgents>
              <NumberOfDeals>8</NumberOfDeals>
              <CurrentPayment>300.00</CurrentPayment>
              <CurrentPaymentPeriod>Jun-26</CurrentPaymentPeriod>
              <PreviousPayment>250.00</PreviousPayment>
              <PreviousPaymentPeriod>May-26</PreviousPaymentPeriod>
              <ForecastedPayment>350.00</ForecastedPayment>
              <YearToDatePayment>550.00</YearToDatePayment>
              <OnFlowDeals>
                <DataPoint><Month>Apr-26</Month><DealCount>3</DealCount></DataPoint>
                <DataPoint><Month>May-26</Month><DealCount>5</DealCount></DataPoint>
              </OnFlowDeals>
              <PaymentHistory>
                <DataPoint><Month>May-26</Month><Amount>250.00</Amount><IsForecasted>false</IsForecasted></DataPoint>
                <DataPoint><Month>Sep-26</Month><Amount>350.00</Amount><IsForecasted>true</IsForecasted></DataPoint>
              </PaymentHistory>
              <Agents>
                <Agent>
                  <AgentId>AG20</AgentId>
                  <AgentName>Eve</AgentName>
                  <Role>Senior Agent</Role>
                  <Email>eve@apexsolutions.example.com</Email>
                  <Phone>555-003-0020</Phone>
                  <ActiveDeals>4</ActiveDeals>
                </Agent>
                <Agent>
                  <AgentId>AG21</AgentId>
                  <AgentName>Frank</AgentName>
                  <Role>Sales Agent</Role>
                  <Email>frank@apexsolutions.example.com</Email>
                  <Phone>555-003-0021</Phone>
                  <ActiveDeals>1</ActiveDeals>
                </Agent>
              </Agents>
            </Agency>
          </Agencies>
        </AgenciesData>
        """;
}
